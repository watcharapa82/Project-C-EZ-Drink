using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EZTicketProject
{
    public partial class DashboardSale : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;

        public DashboardSale(string roleFromDatabase, int userId, string userName)
        {
            InitializeComponent();
            currentUserRole = roleFromDatabase;
            currentUserId = userId;
            currentUserName = userName;
            CheckAdminAccess();
            LoadAllSales();   
            LoadTopProducts(); 
            LoadTotalRevenue();
        }
        public class UserModel
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Sex { get; set; }
            public string Age { get; set; }
            public string Role { get; set; }
            public string ImagePath { get; set; }
        }

        private void LoadAllSales()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    o.OrderId,
                    o.UserId,
                    u.name AS CustomerName,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt
                FROM Orders o
                LEFT JOIN users u ON o.UserId = u.id
                WHERE o.Status = 'Approved'
                ORDER BY o.CreatedAt DESC";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgSales.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales: " + ex.Message);
            }
        }
        private void LoadTopProducts()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT 
                oi.ProductName,
                SUM(oi.Quantity) AS TotalSold,
                SUM(oi.Quantity * oi.Price) AS TotalRevenue
            FROM OrderItems oi
            INNER JOIN Orders o ON oi.OrderId = o.OrderId
            WHERE o.Status = 'Approved'
            GROUP BY oi.ProductName
            ORDER BY TotalSold DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgTopProducts.ItemsSource = dt.DefaultView;
            }
        }
        private void LoadTopProductsByDate(DateTime start, DateTime end)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT 
                oi.ProductName,
                SUM(oi.Quantity) AS TotalSold,
                SUM(oi.Quantity * oi.Price) AS TotalRevenue
            FROM OrderItems oi
            INNER JOIN Orders o ON oi.OrderId = o.OrderId
            WHERE o.Status = 'Approved'
              AND o.CreatedAt BETWEEN @start AND @end
            GROUP BY oi.ProductName
            ORDER BY TotalSold DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgTopProducts.ItemsSource = dt.DefaultView;
            }
        }

        private void LoadTotalRevenue()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT SUM(TotalPrice) 
            FROM Orders 
            WHERE Status = 'Approved'";

                SqlCommand cmd = new SqlCommand(query, conn);

                object result = cmd.ExecuteScalar();

                decimal total = (result != DBNull.Value) ? Convert.ToDecimal(result) : 0;

                txtTotalRevenue.Text = $"{total:N2} บาท";
            }
        }

        public ObservableCollection<string> FilteredShows { get; set; }
        private void CheckAdminAccess()
        {
            if (btnAdminMenu != null)
                btnAdminMenu.Visibility = Visibility.Visible;

            if (currentUserRole != "Admin")
            {
                if (AdminSubMenuContainer != null)
                    AdminSubMenuContainer.Visibility = Visibility.Collapsed;
            }
        }
        private void BtnOrganizer_Click(object sender, RoutedEventArgs e)
        {
            ProductManagement productManagement = new ProductManagement();
            productManagement.Show();
            this.Close();
        }
        private void BtnAdminMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'สำหรับ Admin หน้า Dashboard Member'");
            return;
        }

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aboutusWindow = new AboutusWindow(currentUserRole, currentUserId, currentUserName);
            aboutusWindow.Show();
            this.Close();
        }
        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
        }
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (dpStart.SelectedDate == null || dpEnd.SelectedDate == null)
            {
                MessageBox.Show("กรุณาเลือกวันเริ่มต้นและวันสิ้นสุด");
                return;
            }

            DateTime start = dpStart.SelectedDate.Value.Date;
            DateTime end = dpEnd.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

            LoadSales(start, end);
            LoadTopProducts(start, end);
            LoadTotalRevenue(start, end);
        }

        private void LoadSales(DateTime start, DateTime end)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"
                SELECT 
                    o.OrderId,
                    o.UserName,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt
                FROM Orders o
                WHERE o.Status = 'Approved'
                AND o.CreatedAt BETWEEN @start AND @end
                ORDER BY o.CreatedAt DESC";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                dgSales.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่พบข้อมูลยอดขายในช่วงเวลานี้");
                }
            }
        }
        private void LoadTopProducts(DateTime start, DateTime end)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"
                SELECT TOP 5 
                    ProductName,
                    SUM(Quantity) AS TotalSold
                FROM OrderItems oi
                INNER JOIN Orders o ON oi.OrderId = o.OrderId
                WHERE o.Status = 'Approved'
                AND o.CreatedAt BETWEEN @start AND @end
                GROUP BY ProductName
                ORDER BY TotalSold DESC";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                dgTopProducts.ItemsSource = dt.DefaultView;
            }
        }
        private void LoadTotalRevenue(DateTime start, DateTime end)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string q = @"
                SELECT SUM(TotalPrice) 
                FROM Orders
                WHERE Status = 'Approved'
                AND CreatedAt BETWEEN @start AND @end";

                SqlCommand cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                object result = cmd.ExecuteScalar();
                decimal total = result == DBNull.Value ? 0 : Convert.ToDecimal(result);

                txtTotalRevenue.Text = $"{total:N2} บาท";
            }
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
        private void BtnFavorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesWindow favorites = new FavoritesWindow();
            favorites.Show();
            Close();
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            Profile profile = new Profile();
            profile.Show();
            Close();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
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
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'Dashboard Sales' ");
        }

        private void BtnCloss_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("ท่านต้องการออกจากระบบหรือไม่?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                UserSession.ClearSession();
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
        }

        private void Bordor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void Bordor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            }
        }
    }
}

