using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace EZTicketProject
{
    public partial class Profile : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole = UserSession.CurrentUserRole;
        private readonly int currentUserId = UserSession.CurrentUserId;
        private readonly string currentUserName = UserSession.CurrentUserName;
        private readonly string currentUserPhone = UserSession.CurrentUserPhone;
        private readonly string currentUserImage = UserSession.CurrentUserImagePath;

        public Profile()
        {
            InitializeComponent();

            if (!UserSession.IsLoggedIn)
            {
                MessageBox.Show("กรุณาเข้าสู่ระบบก่อน");
                MainWindow mw = new MainWindow();
                mw.Show();
                this.Close();
                return;
            }

            LoadUserInfo();
            LoadUserOrders();
            CheckAdminAccess();
        }
        public class OrderCheckoutItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }


        private void LoadUserInfo()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                SELECT name, phone, role, sex, age, ImagePath
                FROM users
                WHERE id = @id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", currentUserId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    txtUserName.Text = reader["name"]?.ToString() ?? "-";
                    txtPhone.Text = reader["phone"]?.ToString() ?? "-";
                    txtRole.Text = reader["role"]?.ToString() ?? "-";

                    txtSex.Text = reader["sex"] == DBNull.Value ? "ไม่ระบุ" : reader["sex"].ToString();
                    txtAge.Text = reader["age"] == DBNull.Value ? "-" : reader["age"].ToString();

                    string path = reader["ImagePath"]?.ToString();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        imgProfile.Source = new BitmapImage(new Uri(path));
                    }
                }
            }
        }

        private void LoadUserOrders()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = "SELECT OrderId, TotalPrice, Status, CreatedAt FROM Orders WHERE UserId = @uid ORDER BY CreatedAt DESC";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@uid", currentUserId);

                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                dgOrders.ItemsSource = dt.DefaultView;
            }
        }
        private void BtnDashboardMember_Click(object sender, RoutedEventArgs e)
        {
            DashboardMember dashboardMember = new DashboardMember(UserSession.CurrentUserRole, UserSession.CurrentUserId, UserSession.CurrentUserName);
            dashboardMember.Show();

            AdminSubMenuContainer.Visibility = Visibility.Collapsed;

            this.Close();
        }

        private void BtnDashboardSales_Click(object sender, RoutedEventArgs e)
        {
            DashboardSale dashboardsale = new DashboardSale(UserSession.CurrentUserRole, UserSession.CurrentUserId, UserSession.CurrentUserName);
            dashboardsale.Show();
            AdminSubMenuContainer.Visibility = Visibility.Collapsed;
            this.Close();
        }
        private void BtnConfirmOrders_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAdmin)
            {
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึง", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ConfirmOrdersWindow win = new ConfirmOrdersWindow();
            win.ShowDialog();
        }

        private void LoadOrderItemsToGrid(int orderId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                SELECT ProductName, Quantity, Price
                FROM OrderItems
                WHERE OrderId = @oid";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@oid", orderId);

                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                dgOrderItems.ItemsSource = dt.DefaultView;
            }
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem == null) return;

            DataRowView row = (DataRowView)dgOrders.SelectedItem;
            int orderId = Convert.ToInt32(row["OrderId"]);

            LoadOrderItemsToGrid(orderId);
            string status = row["Status"].ToString();

            btnReUploadSlip.Visibility = Visibility.Collapsed;
            btnReOrder.Visibility = Visibility.Collapsed;
            btnDownloadPDF.Visibility = Visibility.Collapsed;

            if (status == "ProblemSlip" || status == "ProblemAmount")
                btnReUploadSlip.Visibility = Visibility.Visible;
            else if (status == "Cancelled")
                btnReOrder.Visibility = Visibility.Visible;
            else if (status == "Approved")
                btnDownloadPDF.Visibility = Visibility.Visible;
        }
        private void BtnReOrder_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow pw = new ProgramWindow();
            pw.Show();
            this.Close();
        }

        private void Bordor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Normal
                    ? WindowState.Maximized
                    : WindowState.Normal;
            }
        }
        private void BtnFavorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesWindow favorites = new FavoritesWindow();
            favorites.Show();
            Close();
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'ข้อมูลส่วนตัว'");
        }

        private void BtnDownloadPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null) return;

            DataRowView row = (DataRowView)dgOrders.SelectedItem;
            int orderId = Convert.ToInt32(row["OrderId"]);
            ReceiptPDFWindow pw = new ReceiptPDFWindow(orderId);
            pw.Show();
            this.Close();
        }

        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow pw = new ProgramWindow();
            pw.Show();
            this.Close();
        }

        private void CheckAdminAccess()
        {
            btnAdminMenu.Visibility = Visibility.Visible;

            AdminSubMenuContainer.Visibility = Visibility.Collapsed;
        }

        private void BtnAdminMenu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUserRole))
            {
                MessageBox.Show("ไม่พบข้อมูลบทบาทผู้ใช้ (Role)");
                return;
            }

            if (!currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึงหน้านี้", "Access Denied",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdminSubMenuContainer.Visibility =
                (AdminSubMenuContainer.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        

        private void BtnOrganizer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentUserRole))
            {
                MessageBox.Show("ไม่พบข้อมูลบทบาทผู้ใช้ (Role)");
                return;
            }

            if (!currentUserRole.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึงหน้านี้", "Access Denied",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProductManagement productmanagent = new ProductManagement();
            productmanagent.Show();
            this.Close();
        }


        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProgramWindow pw = new ProgramWindow();
            pw.Show();
            this.Close();
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aw = new AboutusWindow(
                currentUserRole,
                currentUserId,
                currentUserName
            );
            aw.Show();
            this.Close();
        }

        private void Bordor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
        }

        private void BtnCloss_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("ต้องการออกจากระบบหรือไม่?", "ยืนยัน", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                UserSession.ClearSession();
                MainWindow mw = new MainWindow();
                mw.Show();
                this.Close();
            }
        }
    }
}
