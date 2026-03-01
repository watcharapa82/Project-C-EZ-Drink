using MahApps.Metro.IconPacks;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EZTicketProject
{
    public partial class FavoritesWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly string currentUserName;
        private readonly int currentUserId;

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();

        public FavoritesWindow()
        {
            InitializeComponent();

            currentUserRole = UserSession.CurrentUserRole;
            currentUserId = UserSession.CurrentUserId;
            currentUserName = UserSession.CurrentUserName;

            if (currentUserId <= 0)
            {
                MessageBox.Show("ไม่พบข้อมูลผู้ใช้ กรุณาเข้าสู่ระบบอีกครั้ง");
                return;
            }

            CheckAdminAccess();
            LoadProducts();
        }

        public class Product
        {
            public int Id { get; set; }
            public string ProductName { get; set; }
            public string Category { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public string ImagePath { get; set; }
            public bool IsFavorite { get; set; }
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
        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
        }
        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProgramWindow programWindow = new ProgramWindow();
            programWindow.Show();
            this.Close();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void LoadProducts()
        {
            Products.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        p.Id,
                        p.ProductName,
                        COALESCE(c.CategoryName, p.Category, N'ไม่ระบุหมวดหมู่') AS Category,
                        p.Price,
                        p.Stock,
                        p.ImagePath
                    FROM Favorites f
                    INNER JOIN Products p ON f.ProductId = p.Id
                    LEFT JOIN Categories c ON p.CategoryId = c.Id
                    WHERE f.UserId = @user
                    ORDER BY p.Id DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", currentUserId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Products.Add(new Product
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ProductName = reader["ProductName"].ToString(),
                                Category = reader["Category"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Stock = Convert.ToInt32(reader["Stock"]),
                                ImagePath = reader["ImagePath"] == DBNull.Value ? null : reader["ImagePath"].ToString(),
                                IsFavorite = true
                            });
                        }
                    }

                    RenderProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการโหลดรายการโปรด: " + ex.Message);
            }
        }

        private void RenderProducts()
        {
            ProductPanel.Children.Clear();

            foreach (var product in Products)
            {
                Border card = new Border
                {
                    Width = 160,
                    Height = 250,
                    CornerRadius = new CornerRadius(10),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.2,
                        BlurRadius = 10
                    }
                };

                Grid contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Image image = new Image
                {
                    Width = 120,
                    Height = 120,
                    Margin = new Thickness(10),
                    Stretch = Stretch.UniformToFill
                };
                if (!string.IsNullOrEmpty(product.ImagePath) && File.Exists(product.ImagePath))
                    image.Source = new BitmapImage(new Uri(product.ImagePath));
                else
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/default_product.png"));

                Grid.SetRow(image, 0);

                TextBlock nameBlock = new TextBlock
                {
                    Text = product.ProductName,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5, 2, 5, 0)
                };
                Grid.SetRow(nameBlock, 1);

                TextBlock priceBlock = new TextBlock
                {
                    Text = $"ราคา {product.Price:N2} บาท",
                    Foreground = new SolidColorBrush(Color.FromRgb(253, 138, 138)),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5, 2, 5, 5)
                };
                Grid.SetRow(priceBlock, 2);

                StackPanel qtyPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Label qtyBlock = new Label
                {
                    Content = "1",
                    Width = 35,
                    Height = 25,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };

                Button btnMinus = new Button
                {
                    Content = "-",
                    Width = 25,
                    Height = 25,
                    FontWeight = FontWeights.Bold
                };
                btnMinus.Click += (s, e) =>
                {
                    int qty = int.Parse(qtyBlock.Content.ToString());
                    if (qty > 1) qtyBlock.Content = (qty - 1).ToString();
                };

                Button btnPlus = new Button
                {
                    Content = "+",
                    Width = 25,
                    Height = 25,
                    FontWeight = FontWeights.Bold
                };
                btnPlus.Click += (s, e) =>
                {
                    int qty = int.Parse(qtyBlock.Content.ToString());
                    if (qty < product.Stock)
                        qtyBlock.Content = (qty + 1).ToString();
                    else
                        MessageBox.Show("จำนวนเกินสต็อก");
                };

                qtyPanel.Children.Add(btnMinus);
                qtyPanel.Children.Add(qtyBlock);
                qtyPanel.Children.Add(btnPlus);
                Grid.SetRow(qtyPanel, 3);

                Button addCartButton = new Button
                {
                    Content = "เพิ่มลงตะกร้า",
                    Background = new SolidColorBrush(Color.FromRgb(253, 138, 138)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(5),
                    Tag = product.Id
                };
                addCartButton.Click += (s, e) =>
                {
                    int qty = int.Parse(qtyBlock.Content.ToString());
                    AddToCart(product.Id, product.ProductName, product.Price * qty);
                };
                Grid.SetRow(addCartButton, 4);

                PackIconMaterial heartIcon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Heart,
                    Width = 20,
                    Height = 20,
                    Foreground = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 5, 0),
                    Cursor = Cursors.Hand,
                    Tag = product.Id
                };
                heartIcon.MouseDown += RemoveFavoriteIcon;
                Grid.SetRow(heartIcon, 0);

                contentGrid.Children.Add(image);
                contentGrid.Children.Add(nameBlock);
                contentGrid.Children.Add(priceBlock);
                contentGrid.Children.Add(qtyPanel);
                contentGrid.Children.Add(addCartButton);
                contentGrid.Children.Add(heartIcon);

                card.Child = contentGrid;
                ProductPanel.Children.Add(card);
            }
        }


        private void RemoveFavoriteIcon(object sender, MouseButtonEventArgs e)
        {
            if (sender is PackIconMaterial icon && icon.Tag is int productId)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM Favorites WHERE UserId = @user AND ProductId = @pid";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@user", currentUserId);
                    cmd.Parameters.AddWithValue("@pid", productId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("ลบออกจากรายการโปรดแล้ว");

                LoadProducts();  
            }
        }

        private void AddToCart(int productId, string productName, decimal price)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                    INSERT INTO Cart (UserId, ProductId, Quantity, TotalPrice)
                    VALUES (@u, @p, 1, @t)";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@u", currentUserId);
                    cmd.Parameters.AddWithValue("@p", productId);
                    cmd.Parameters.AddWithValue("@t", price);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"เพิ่ม {productName} ลงตะกร้าแล้ว!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aboutus = new AboutusWindow(currentUserRole, currentUserId, currentUserName);
            aboutus.Show();
            this.Close();
        }
        private void BtnFavorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesWindow favorites = new FavoritesWindow();
            favorites.Show();
            Close();
        }
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            Profile p = new Profile();
            p.Show();
            this.Close();
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

    }
}
