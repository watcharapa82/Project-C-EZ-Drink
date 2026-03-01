using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EZTicketProject
{
    public partial class Cart : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public class CartItem : INotifyPropertyChanged
        {
            public int Id { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public string ImagePath { get; set; }

            private int _quantity;
            public int Quantity
            {
                get => _quantity;
                set
                {
                    if (_quantity != value)
                    {
                        _quantity = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ItemSubtotal));
                    }
                }
            }

            public decimal ItemSubtotal => Price * Quantity;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;

        public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public Cart()
        {
            InitializeComponent();

            if (!UserSession.IsLoggedIn)
            {
                MessageBox.Show("กรุณาเข้าสู่ระบบก่อน");
                MainWindow mw = new MainWindow();
                mw.Show();
                Close();
                return;
            }

            currentUserRole = UserSession.CurrentUserRole;
            currentUserId = UserSession.CurrentUserId;
            currentUserName = UserSession.CurrentUserName;

            this.DataContext = this;

            CheckAdminAccess();

            CartItems.CollectionChanged += (s, e) => CalculateTotal();

            LoadCartItems();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        private void CheckAdminAccess()
        {
            btnAdminMenu.Visibility = Visibility.Visible;

            AdminSubMenuContainer.Visibility = Visibility.Collapsed;
        }

        private void LoadCartItems()
        {
            CartItems.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT p.Id, p.ProductName, p.Price, c.Quantity, p.ImagePath
                        FROM Cart c
                        JOIN Products p ON c.ProductId = p.Id
                        WHERE c.UserId = @uid";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@uid", currentUserId);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        CartItems.Add(new CartItem
                        {
                            Id = reader.GetInt32(0),
                            ProductName = reader["ProductName"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            ImagePath = reader["ImagePath"].ToString()
                        });
                    }
                }

                CalculateTotal();

                foreach (var item in CartItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("โหลดตะกร้าล้มเหลว: " + ex.Message);
            }
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity))
                CalculateTotal();
        }

        private void CalculateTotal()
        {
            TotalAmount = CartItems.Sum(i => i.ItemSubtotal);
        }

        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var win = new ProgramWindow();
            win.Show();
            Close();
        }

        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProgramWindow();
            win.Show();
            Close();
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


        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("คุณอยู่ที่หน้าตะกร้าแล้ว");
        }

        private void BtnCloss_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("ต้องการออกจากระบบหรือไม่?", "ยืนยัน", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                UserSession.ClearSession();
                MainWindow mw = new MainWindow();
                mw.Show();
                Close();
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

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow win = new AboutusWindow(currentUserRole, currentUserId, currentUserName);
            win.Show();
            Close();
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
        private void Bordor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Bordor_MouseLeftButtondown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = (WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is CartItem item)
            {
                if (MessageBox.Show($"ต้องการลบ {item.ProductName} หรือไม่?",
                    "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;

                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string q = "DELETE FROM Cart WHERE UserId=@uid AND ProductId=@pid";
                        SqlCommand cmd = new SqlCommand(q, conn);
                        cmd.Parameters.AddWithValue("@uid", currentUserId);
                        cmd.Parameters.AddWithValue("@pid", item.Id);
                        cmd.ExecuteNonQuery();
                    }

                    CartItems.Remove(item);
                    CalculateTotal();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ลบสินค้าไม่สำเร็จ:\n" + ex.Message);
                }
            }
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (!CartItems.Any())
            {
                MessageBox.Show("ตะกร้าว่าง");
                return;
            }

            CheckoutWindow win = new CheckoutWindow(CartItems.ToList(), TotalAmount);
            win.ShowDialog();
        }

    }
}
