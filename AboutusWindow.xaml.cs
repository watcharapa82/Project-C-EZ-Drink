using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace EZTicketProject
{
    public partial class AboutusWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;
        public AboutusWindow(string roleFromDatabase, int userId, string userName)
        {
            InitializeComponent();
            currentUserRole = roleFromDatabase;
            currentUserId = userId;
            currentUserName = userName;
            CheckAdminAccess();
        }

        public string UserRole { get; set; } = "User";


        private void CheckAdminAccess()
        {
            btnAdminMenu.Visibility = Visibility.Visible;

            AdminSubMenuContainer.Visibility = Visibility.Collapsed;
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
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
        }
        
        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'เกี่ยวกับเรา'");
            return;
        }
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
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
        private void BtnFavorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesWindow favorites = new FavoritesWindow();
            favorites.Show();
            Close();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            Profile profile = new Profile();
            profile.Show();
            Close();
        }
        private void Bordor_MouseLeftButtondown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Normal
                    ? WindowState.Maximized
                    : WindowState.Normal;
            }
            else
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.DragMove();
                }
            }
        }
    }
}

