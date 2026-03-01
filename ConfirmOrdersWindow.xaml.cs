using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EZTicketProject
{
    public partial class ConfirmOrdersWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private int selectedOrderId = -1;
        public int CurrentOrderId { get; private set; } = -1;

        public ConfirmOrdersWindow()
        {
            InitializeComponent();
            LoadPendingOrders();
        }

        private void LoadPendingOrders()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"SELECT OrderId, UserId, UserName, Phone, TotalPrice, SlipImagePath, Status, CreatedAt 
                FROM Orders WHERE Status NOT IN ('Approved', 'Cancelled')
                ORDER BY CreatedAt DESC";

                SqlDataAdapter da = new SqlDataAdapter(q, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgOrders.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0)
                {
                    dgOrderItems.ItemsSource = null;

                    ClearDetails();

                    MessageBox.Show(
                        "ยังไม่มีรายการสินค้ารออนุมัติ",
                        "แจ้งเตือน",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }
        private void UpdateProductStock(int orderId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // ดึงรายการสินค้าในออเดอร์
                string selectQuery = @"
            SELECT ProductId, Quantity
            FROM OrderItems
            WHERE OrderId = @oid";

                SqlCommand selectCmd = new SqlCommand(selectQuery, conn);
                selectCmd.Parameters.AddWithValue("@oid", orderId);

                SqlDataReader reader = selectCmd.ExecuteReader();

                List<(int ProductId, int Qty)> items = new List<(int, int)>();

                while (reader.Read())
                {
                    items.Add((
                        Convert.ToInt32(reader["ProductId"]),
                        Convert.ToInt32(reader["Quantity"])
                    ));
                }

                reader.Close();

                foreach (var item in items)
                {
                    string updateQuery = @"
                UPDATE Products
                SET Stock = Stock - @qty
                WHERE Id = @pid";

                    SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@qty", item.Qty);
                    updateCmd.Parameters.AddWithValue("@pid", item.ProductId);

                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        private void BtnReportIssue_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "เลือกปัญหาที่พบ:\nYES = สลิปไม่ถูกต้อง\nNO = จำนวนเงินไม่ถูกต้อง",
                "แจ้งปัญหา",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                UpdateOrderStatus(CurrentOrderId, "ProblemSlip");
                MessageBox.Show("อัพเดตสถานะ: สลิปไม่ถูกต้อง (รอผู้ใช้อัพโหลดใหม่)");
            }
            else if (result == MessageBoxResult.No)
            {
                UpdateOrderStatus(CurrentOrderId, "ProblemAmount");
                MessageBox.Show("อัพเดตสถานะ: จำนวนเงินผิด (รอผู้ใช้ชำระใหม่)");
            }

            this.Close();
        }

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("ต้องการยกเลิกออเดอร์นี้หรือไม่?",
                "ยืนยัน", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                == MessageBoxResult.Yes)
            {
                UpdateOrderStatus(CurrentOrderId, "Cancelled");
                MessageBox.Show("ออเดอร์ถูกยกเลิกแล้ว");
                this.Close();
            }
        }
        private void ClearDetails()
        {
            txtUserName.Text = "User: -";
            txtPhone.Text = "Phone: -";
            txtFinalTotal.Text = "Total: -";
            imgSlip.Source = null;
            dgOrderItems.ItemsSource = null;
            selectedOrderId = -1;
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearDetails();
            if (dgOrders.SelectedItem == null) return;

            DataRowView row = (DataRowView)dgOrders.SelectedItem;
            selectedOrderId = Convert.ToInt32(row["OrderId"]);
            CurrentOrderId = selectedOrderId;

            txtUserName.Text = "User: " + row["UserName"].ToString();
            txtPhone.Text = "Phone: " + row["Phone"].ToString();
            txtFinalTotal.Text = "Total: " + Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + " บาท";

            string slip = row["SlipImagePath"] == DBNull.Value ? null : row["SlipImagePath"].ToString();
            if (!string.IsNullOrEmpty(slip) && File.Exists(slip))
            {
                imgSlip.Source = new BitmapImage(new Uri(slip));
            }

            LoadOrderItems(selectedOrderId);
        }

        private void LoadOrderItems(int orderId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"
            SELECT 
                ProductName,
                Quantity,
                Price,
                (Price * Quantity) AS ItemSubtotal,
                (Price * Quantity) / 1.07 AS PriceBeforeVat,
                (Price * Quantity) - ((Price * Quantity) / 1.07) AS VatAmount
            FROM OrderItems
            WHERE OrderId = @oid";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@oid", orderId);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgOrderItems.ItemsSource = dt.DefaultView;

                decimal sumBeforeVat = 0, sumVat = 0, sumFinal = 0;

                foreach (DataRow r in dt.Rows)
                {
                    int qty = Convert.ToInt32(r["Quantity"]);
                    decimal price = Convert.ToDecimal(r["Price"]);
                    decimal subtotal = Convert.ToDecimal(r["ItemSubtotal"]);
                    decimal beforeVat = Convert.ToDecimal(r["PriceBeforeVat"]);
                    decimal vatAmount = Convert.ToDecimal(r["VatAmount"]);

                    sumBeforeVat += beforeVat;
                    sumVat += vatAmount;
                    sumFinal += subtotal;
                }

                txtPriceBeforeVat.Text = $"{sumBeforeVat:N2} บาท";
                txtVat.Text = $"{sumVat:N2} บาท";
                txtFinalTotal.Text = $"{sumFinal:N2} บาท";
            }
        }


        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrderId <= 0)
            {
                MessageBox.Show("กรุณาเลือกออเดอร์");
                return;
            }

            if (MessageBox.Show("ยืนยันอนุมัติออเดอร์นี้หรือไม่?", "ยืนยัน", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                UpdateProductStock(selectedOrderId);   // ✔ ลดสต็อกจริง
                UpdateOrderStatus(selectedOrderId, "Approved");

                MessageBox.Show("อนุมัติออเดอร์แล้ว");

                ConfirmOrdersWindow rw = new ConfirmOrdersWindow();
                rw.ShowDialog();

                LoadPendingOrders();
            }
        }


        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrderId <= 0)
            {
                MessageBox.Show("กรุณาเลือกออเดอร์");
                return;
            }

            if (MessageBox.Show("ต้องการยกเลิกออเดอร์นี้หรือไม่?", "ยืนยัน", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                UpdateOrderStatus(selectedOrderId, "Cancelled");
                MessageBox.Show("ออเดอร์ถูกยกเลิกแล้ว");

                LoadPendingOrders();
            }
        }

        private void BtnProblem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrderId <= 0)
            {
                MessageBox.Show("กรุณาเลือกออเดอร์");
                return;
            }

            var res = MessageBox.Show(
                "เลือกปัญหา:\nYES = สลิปไม่ถูกต้อง\nNO = จำนวนเงินไม่ถูกต้อง",
                "แจ้งปัญหา",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                UpdateOrderStatus(selectedOrderId, "ProblemSlip");
                MessageBox.Show("อัพเดตสถานะ: สลิปไม่ถูกต้อง");
            }
            else if (res == MessageBoxResult.No)
            {
                UpdateOrderStatus(selectedOrderId, "ProblemAmount");
                MessageBox.Show("อัพเดตสถานะ: จำนวนเงินไม่ถูกต้อง");
            }

            LoadPendingOrders();
        }

        private void UpdateOrderStatus(int orderId, string status)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = "UPDATE Orders SET Status = @s WHERE OrderId = @id";
                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.ExecuteNonQuery();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
