using MahApps.Metro.IconPacks;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.ApplicationServices;
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
    public partial class ProgramWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly string currentUserName;
        private readonly int currentUserId;

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();

        public ProgramWindow()
        {
            InitializeComponent();

            currentUserRole = UserSession.CurrentUserRole;
            currentUserId = UserSession.CurrentUserId;
            currentUserName = UserSession.CurrentUserName;

            CheckAdminAccess();
            LoadCategories();
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


        private void BtnMain_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่หน้าแรก");
        }

        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aboutus = new AboutusWindow(currentUserRole, currentUserId,currentUserName);
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

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
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
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึง");
                return;
            }

            ConfirmOrdersWindow win = new ConfirmOrdersWindow();
            win.ShowDialog();
        }

        private void LoadProducts(string category = "All")
        {
            Products.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT 
                p.Id, 
                p.ProductName,
                COALESCE(c.CategoryName, p.Category) AS Category,
                p.Price, 
                p.Stock, 
                p.ImagePath,
                CASE WHEN f.ProductId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            LEFT JOIN Favorites f ON p.Id = f.ProductId AND f.UserId = @uid
            WHERE p.Stock > 0
        ";

                if (category != "All")
                {
                    query += " AND COALESCE(c.CategoryName, p.Category) = @cat ";
                }

                query += " ORDER BY p.Id DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@uid", currentUserId);

                if (category != "All")
                    cmd.Parameters.AddWithValue("@cat", category);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Products.Add(new Product
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        ProductName = r["ProductName"].ToString(),
                        Category = r["Category"].ToString(),
                        Price = Convert.ToDecimal(r["Price"]),
                        Stock = Convert.ToInt32(r["Stock"]),
                        ImagePath = r["ImagePath"]?.ToString(),
                        IsFavorite = Convert.ToInt32(r["IsFavorite"]) == 1
                    });
                }
            }

            RenderProducts();
        }


        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilter.SelectedItem == null) return;

            string selected = CategoryFilter.SelectedItem.ToString();
            LoadProducts(selected);
        }

        private void RenderProducts()
        {
            ProductPanel.Children.Clear();

            foreach (var product in Products)
            {
                Border card = new Border
                {
                    Width = 160,
                    Height = 300,
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 270,
                        ShadowDepth = 2,
                        Opacity = 0.2
                    },
                    Tag = product.Id
                };

                Grid contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 40 });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Add to cart button

                Image image = new Image
                {
                    Width = 100,
                    Height = 100,
                    Margin = new Thickness(10),
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(image, 0);

                if (!string.IsNullOrEmpty(product.ImagePath) && File.Exists(product.ImagePath))
                {
                    image.Source = new BitmapImage(new Uri(product.ImagePath));
                }
                else
                {
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/default_product.png"));
                }

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
                    Text = $"ราคา: {product.Price:N2} บาท",
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5, 2, 5, 5)
                };
                Grid.SetRow(priceBlock, 2);

                StackPanel qtyPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 5)
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
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2),
                    Tag = product.Id
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
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2),
                    Tag = product.Id
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

                Button cartButton = new Button
                {
                    Content = product.Stock > 0 ? "เพิ่มลงตะกร้า" : "สินค้าหมด",
                    Background = product.Stock > 0
        ? new SolidColorBrush(Color.FromRgb(253, 138, 138))
        : Brushes.Gray,
                    Foreground = Brushes.White,
                    Margin = new Thickness(5, 0, 5, 10),
                    Padding = new Thickness(5),
                    Tag = product.Id,
                    IsEnabled = product.Stock > 0
                };

                Grid.SetRow(cartButton, 4);

                cartButton.Click += (s, e) =>
                {
                    int qty = int.Parse(qtyBlock.Content.ToString());
                    AddToCart(product.Id, product.ProductName, product.Price * qty);
                };

                PackIconMaterial favoriteIcon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Heart,
                    Width = 20,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 5, 0),
                    Foreground = product.IsFavorite ? Brushes.Red : Brushes.Gray,
                    Tag = product.Id,
                    Cursor = Cursors.Hand
                };
                favoriteIcon.MouseDown += FavoriteIcon_MouseDown;

                Grid.SetRow(favoriteIcon, 0);

                contentGrid.Children.Add(image);
                contentGrid.Children.Add(nameBlock);
                contentGrid.Children.Add(priceBlock);
                contentGrid.Children.Add(qtyPanel);
                contentGrid.Children.Add(cartButton);
                contentGrid.Children.Add(favoriteIcon);

                card.Child = contentGrid;
                ProductPanel.Children.Add(card);
            }
        }
        private void FavoriteIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is PackIconMaterial icon && icon.Tag is int productId)
            {
                ToggleFavorite(icon, productId);
            }
        }

        private void AddToCart(int productId, string productName, decimal productPrice)
        {
            try
            {
                if (currentUserId <= 0)
                {
                    MessageBox.Show("กรุณาเข้าสู่ระบบก่อน");
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string stockQuery = "SELECT Stock FROM Products WHERE Id = @pid";
                    SqlCommand stockCmd = new SqlCommand(stockQuery, conn);
                    stockCmd.Parameters.AddWithValue("@pid", productId);
                    int stock = Convert.ToInt32(stockCmd.ExecuteScalar());

                    if (stock <= 0)
                    {
                        MessageBox.Show("สินค้าหมดสต็อก", "แจ้งเตือน", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string checkQuery = @"
                SELECT Quantity 
                FROM Cart 
                WHERE UserId = @uid AND ProductId = @pid";

                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@uid", currentUserId);
                    checkCmd.Parameters.AddWithValue("@pid", productId);

                    object result = checkCmd.ExecuteScalar();

                    if (result != null)
                    {
                        int currentQty = Convert.ToInt32(result);

                        if (currentQty + 1 > stock)
                        {
                            MessageBox.Show($"มีในสต็อกเพียง {stock} ชิ้น", "สต็อกไม่เพียงพอ");
                            return;
                        }

                        string updateQuery = @"
                    UPDATE Cart 
                    SET Quantity = Quantity + 1,
                        TotalPrice = TotalPrice + @price
                    WHERE UserId = @uid AND ProductId = @pid";

                        SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@uid", currentUserId);
                        updateCmd.Parameters.AddWithValue("@pid", productId);
                        updateCmd.Parameters.AddWithValue("@price", productPrice);
                        updateCmd.ExecuteNonQuery();

                        MessageBox.Show($"เพิ่มจำนวน {productName} เป็น {currentQty + 1} ชิ้นแล้ว!");
                    }
                    else
                    {
                        string insertQuery = @"
                    INSERT INTO Cart (UserId, ProductId, Quantity, TotalPrice)
                    VALUES (@uid, @pid, 1, @price)";

                        SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@uid", currentUserId);
                        insertCmd.Parameters.AddWithValue("@pid", productId);
                        insertCmd.Parameters.AddWithValue("@price", productPrice);
                        insertCmd.ExecuteNonQuery();

                        MessageBox.Show($"เพิ่ม {productName} ลงในตะกร้าแล้ว!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message);
            }
        }

        private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag is int productId)
            {
                var product = Products.FirstOrDefault(p => p.Id == productId);
                if (product == null) return;

                var dialog = new Window
                {
                    Title = "เพิ่มลงตะกร้า",
                    Width = 300,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize,
                    Owner = this,
                    WindowStyle = WindowStyle.ToolWindow
                };

                var panel = new StackPanel { Margin = new Thickness(20) };
                panel.Children.Add(new TextBlock
                {
                    Text = $"ระบุจำนวนของ {product.ProductName} (คงเหลือ {product.Stock})",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var qtyBox = new TextBox
                {
                    Text = "1",
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };

                var btnAdd = new Button
                {
                    Content = "เพิ่มลงตะกร้า",
                    Background = new SolidColorBrush(Color.FromRgb(253, 138, 138)),
                    Foreground = Brushes.White
                };

                btnAdd.Click += (s, ev) =>
                {
                    if (int.TryParse(qtyBox.Text, out int qty) && qty > 0)
                    {
                        if (qty > product.Stock)
                        {
                            MessageBox.Show($"มีในสต็อกเพียง {product.Stock} ชิ้น", "สต็อกไม่เพียงพอ");
                            return;
                        }

                        MessageBox.Show($"เพิ่ม {product.ProductName} จำนวน {qty} ชิ้นลงในตะกร้าแล้ว!");
                        AddToCart(product.Id, product.ProductName, product.Price * qty);
                        dialog.Close();
                    }
                    else
                    {
                        MessageBox.Show("กรุณากรอกจำนวนที่ถูกต้อง");
                    }
                };

                panel.Children.Add(qtyBox);
                panel.Children.Add(btnAdd);
                dialog.Content = panel;
                dialog.ShowDialog();
            }
        }
        private void LoadCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT DISTINCT Category FROM Products ORDER BY Category ASC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader r = cmd.ExecuteReader();

                    CategoryFilter.Items.Clear();
                    CategoryFilter.Items.Add("All");

                    while (r.Read())
                    {
                        string cat = r["Category"].ToString();
                        if (!string.IsNullOrWhiteSpace(cat))
                            CategoryFilter.Items.Add(cat);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("โหลดหมวดหมู่ล้มเหลว: " + ex.Message);
            }

            CategoryFilter.SelectedIndex = 0;
        }

        private void ToggleFavorite(PackIconMaterial icon, int productId)
        {
            bool isFavorite = icon.Foreground == Brushes.Red;

            if (isFavorite)
            {
                icon.Foreground = Brushes.Gray;
                MessageBox.Show("ลบออกจากรายการโปรดแล้ว");
                RemoveFavorite(productId);
            }
            else
            {
                icon.Foreground = Brushes.Red;
                MessageBox.Show("เพิ่มในรายการโปรดแล้ว");
                AddFavorite(productId);
            }
        }

        private void AddFavorite(int productId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Favorites (UserId, ProductId) VALUES (@user, @pid)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", currentUserId);
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.ExecuteNonQuery();
            }
        }

        private void RemoveFavorite(int productId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Favorites WHERE UserId=@user AND ProductId=@pid";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", currentUserId);
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.ExecuteNonQuery();
            }
        }

    }
}