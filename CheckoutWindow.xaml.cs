using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using static EZTicketProject.Cart;

namespace EZTicketProject
{
    public partial class CheckoutWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        public List<CartItem> CartItems { get; set; }
        public decimal TotalAmount { get; set; }

        private string slipPath = null;
        public int CurrentOrderId { get; set; }


        public CheckoutWindow(List<CartItem> cartItems, decimal total)
        {
            InitializeComponent();

            CartItems = cartItems;
            TotalAmount = total;

            LoadCartData();
        }

        private void LoadCartData()
        {
            txtTotal.Text = $"{TotalAmount:N2} บาท";

            decimal priceBeforeVat = TotalAmount / 1.07m;
            decimal vat = TotalAmount - priceBeforeVat;

            txtPriceBeforeVat.Text = $"{priceBeforeVat:N2} บาท";
            txtVat.Text = $"{vat:N2} บาท";

            DataTable dt = new DataTable();
            dt.Columns.Add("ProductName");
            dt.Columns.Add("Quantity");
            dt.Columns.Add("Price");

            foreach (var item in CartItems)
            {
                dt.Rows.Add(
                    item.ProductName,
                    item.Quantity,
                    $"{item.ItemSubtotal:N2}"   
                );
            }

            dgItems.ItemsSource = dt.DefaultView;

            txtOrderInfo.Text =
                $"ผู้สั่งซื้อ: {UserSession.CurrentUserName}\n" +
                $"เบอร์โทร: {UserSession.UserPhone}\n" +
                $"ยอดสุทธิ: {TotalAmount:N2} บาท\n" +
                $"วันที่: {DateTime.Now:dd/MM/yyyy HH:mm}";
        }

        private void BtnUploadSlip_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png";

            if (dlg.ShowDialog() == true)
            {
                slipPath = dlg.FileName;
                imgSlip.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(slipPath));
            }
        }

        private void BtnSubmitOrder_Click(object sender, RoutedEventArgs e)
        {
            if (slipPath == null)
            {
                MessageBox.Show("กรุณาอัพโหลดสลิปก่อน!", "แจ้งเตือน");
                return;
            }

            string savedSlip = SaveSlip();

            int newOrderId = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"INSERT INTO Orders 
                           (UserId, UserName, Phone, TotalPrice, SlipImagePath, Status, CreatedAt)
                           OUTPUT INSERTED.OrderId
                           VALUES (@uid, @un, @ph, @total, @slip, 'Pending', GETDATE())";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@uid", UserSession.CurrentUserId);
                cmd.Parameters.AddWithValue("@un", UserSession.CurrentUserName);
                cmd.Parameters.AddWithValue("@ph", UserSession.UserPhone);
                cmd.Parameters.AddWithValue("@total", TotalAmount);
                cmd.Parameters.AddWithValue("@slip", savedSlip);

                newOrderId = (int)cmd.ExecuteScalar();

                foreach (var item in CartItems)
                {
                    string qi = @"INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, Price)
                                  VALUES (@oid, @pid, @pn, @qty, @price)";

                    SqlCommand ci = new SqlCommand(qi, conn);
                    ci.Parameters.AddWithValue("@oid", newOrderId);
                    ci.Parameters.AddWithValue("@pid", item.Id);
                    ci.Parameters.AddWithValue("@pn", item.ProductName);
                    ci.Parameters.AddWithValue("@qty", item.Quantity);
                    ci.Parameters.AddWithValue("@price", item.Price);
                    ci.ExecuteNonQuery();
                }

                SqlCommand clear = new SqlCommand("DELETE FROM Cart WHERE UserId=@uid", conn);
                clear.Parameters.AddWithValue("@uid", UserSession.CurrentUserId);
                clear.ExecuteNonQuery();
            }

            MessageBox.Show("ส่งคำสั่งซื้อแล้ว รอแอดมินตรวจสอบ", "สำเร็จ");
            this.Close();
        }

        private string SaveSlip()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SlipImages");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string file = "slip_" + DateTime.Now.Ticks + Path.GetExtension(slipPath);
            string dest = Path.Combine(dir, file);

            File.Copy(slipPath, dest, true);

            return dest;
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

        private void UpdateOrderStatus(int orderId, string status)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = "UPDATE Orders SET Status = @status WHERE OrderId = @id";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", orderId);

                cmd.ExecuteNonQuery();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
