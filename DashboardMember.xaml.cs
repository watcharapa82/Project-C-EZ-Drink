using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EZTicketProject
{
    public partial class DashboardMember : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";
        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;

        public DashboardMember(string roleFromDatabase, int userId, string userName)
        {
            InitializeComponent();

            currentUserRole = roleFromDatabase;
            currentUserId = userId;
            currentUserName = userName;

            CheckAdminAccess();

            // โหลดข้อมูล ALL ตั้งแต่เปิดหน้า
            LoadUsers("All");
        }

        // ===== MODEL สำหรับ XAML =====
        public class UserModel
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Sex { get; set; }
            public string Age { get; set; }
            public string Role { get; set; }
            public string ImagePath { get; set; }
        }


        // ========== ตรวจสอบสิทธิ์ Admin ==============
        private void CheckAdminAccess()
        {
            btnAdminMenu.Visibility = Visibility.Visible;

            if (currentUserRole != "Admin")
                AdminSubMenuContainer.Visibility = Visibility.Collapsed;
        }

        // ========== โหลดเฉพาะ Role ====================
        private void LoadUsersByRole(string role)
        {
            LoadUsers(role);
        }

        // ========== โหลด Users ทั้งหมดหรือเฉพาะ role ============
        private void LoadUsers(string role)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query;

                    if (role == "All")
                    {
                        query = @"SELECT name, phone, sex, age, role, ImagePath 
                                  FROM users ORDER BY id ASC";
                    }
                    else
                    {
                        query = @"SELECT name, phone, sex, age, role, ImagePath 
                                  FROM users WHERE role = @role ORDER BY id ASC";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);

                    if (role != "All")
                        cmd.Parameters.AddWithValue("@role", role);

                    SqlDataReader reader = cmd.ExecuteReader();

                    var list = new List<UserModel>();

                    while (reader.Read())
                    {
                        string img = reader["ImagePath"]?.ToString();
                        if (string.IsNullOrWhiteSpace(img) || !File.Exists(img))
                            img = "/Images/Profile.png";

                        list.Add(new UserModel
                        {
                            Name = reader["name"].ToString(),
                            Phone = reader["phone"].ToString(),
                            Sex = reader["sex"]?.ToString() ?? "ไม่ระบุ",
                            Age = reader["age"]?.ToString() ?? "-",
                            Role = reader["role"].ToString(),
                            ImagePath = img
                        });
                    }

                    userCards.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการโหลดข้อมูลสมาชิก:\n" + ex.Message);
            }
        }


        private void BtnAll_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers("All");
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersByRole("User");
        }

        private void BtnAdmins_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersByRole("Admin");
        }
        private void BtnDashboardMember_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'Dashboard Member'");
            return;
        }

        private void BtnDashboardSales_Click(object sender, RoutedEventArgs e)
        {
            DashboardSale dashboardsale = new DashboardSale(UserSession.CurrentUserRole, UserSession.CurrentUserId, UserSession.CurrentUserName);
            dashboardsale.Show();
            AdminSubMenuContainer.Visibility = Visibility.Collapsed;
            this.Close();
        }
        private void BtnOrganizer_Click(object sender, RoutedEventArgs e)
        {
            ProductManagement productManagement = new ProductManagement();
            productManagement.Show();
            this.Close();
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

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aboutus = new AboutusWindow(currentUserRole, currentUserId, currentUserName);
            aboutus.Show();
            this.Close();
        }

        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow pw = new ProgramWindow();
            pw.Show();
            this.Close();
        }

        private void BtnFavorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesWindow f = new FavoritesWindow();
            f.Show();
            this.Close();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            Profile p = new Profile();
            p.Show();
            this.Close();
        }

        private void BtnCloss_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("ท่านต้องการออกจากระบบหรือไม่?", "ยืนยัน", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                UserSession.ClearSession();
                MainWindow mw = new MainWindow();
                mw.Show();
                this.Close();
            }
        }

        private void BtnConfirmOrders_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAdmin)
            {
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึง");
                return;
            }

            ConfirmOrdersWindow win = new ConfirmOrdersWindow();
            win.ShowDialog();
        }
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProgramWindow pw = new ProgramWindow();
            pw.Show();
            this.Close();
        }

        private void Bordor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Bordor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState =
                    WindowState == WindowState.Normal ?
                    WindowState.Maximized : WindowState.Normal;
            }
        }
    }
}
