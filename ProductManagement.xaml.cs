using Microsoft.VisualBasic;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EZTicketProject
{
    public partial class ProductManagement : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();

        public ProductManagement()
        {
            InitializeComponent();

            if (!UserSession.IsLoggedIn)
            {
                MessageBox.Show("กรุณาเข้าสู่ระบบก่อนใช้งานระบบจัดการสินค้า", "แจ้งเตือน");
                MainWindow login = new MainWindow();
                login.Show();
                this.Close();
                return;
            }

            currentUserRole = UserSession.CurrentUserRole;
            currentUserId = UserSession.CurrentUserId;
            currentUserName = UserSession.CurrentUserName;

            CheckAdminAccess();
            LoadProducts();
            LoadCategories();

            dgProducts.ItemsSource = Products;
        }

        public class Product
        {
            public int Id { get; set; }
            public string ProductName { get; set; }
            public string Category { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public int SalesQuantity { get; set; }   
            public decimal SalesAmount { get; set; }
            public string ImagePath { get; set; }
        }

        private string selectedImagePath = null;

        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (dlg.ShowDialog() == true)
            {
                selectedImagePath = dlg.FileName;
                imgPreview.Source = new BitmapImage(new Uri(selectedImagePath));
            }
        }
        private void BtnCloss_Click(object sender, RoutedEventArgs e)
        {
            UserSession.ClearSession();
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void LoadCategories()
        {
            cmbCategory.Items.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT DISTINCT Category FROM Products";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cmbCategory.Items.Add(reader["Category"].ToString());
                }
            }
        }

        private void LoadProducts()
        {
            Products.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Id, ProductName, Category, Price, Stock, SalesQuantity, SalesAmount, ImagePath FROM Products ORDER BY Id ASC";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Products.Add(new Product
                    {
                        Id = (int)reader["Id"],
                        ProductName = reader["ProductName"].ToString(),
                        Category = reader["Category"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        Stock = Convert.ToInt32(reader["Stock"]),
                        ImagePath = reader["ImagePath"] == DBNull.Value ? null : reader["ImagePath"].ToString()
                    });
                }
            }
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var prompt = new Window
            {
                Title = "เพิ่ม Category",
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = this,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Background = System.Windows.Media.Brushes.White
            };

            var stack = new StackPanel { Margin = new Thickness(5) };
            var textBlock = new TextBlock { Text = "กรอกชื่อหมวดหมู่ใหม่:", Margin = new Thickness(0, 0, 0, 8) };
            var textBox = new TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 10), Foreground = System.Windows.Media.Brushes.Black };
            var button = new Button { Content = "ตกลง", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };

            string newCategory = null;

            button.Click += (s, ev) =>
            {
                string input = textBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("กรุณาป้อนชื่อหมวดหมู่", "ข้อผิดพลาด", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; 
                }

                newCategory = input;
                prompt.DialogResult = true; 
                prompt.Close();
            };

            textBox.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };

            stack.Children.Add(textBlock);
            stack.Children.Add(textBox);
            stack.Children.Add(button);
            prompt.Content = stack;

            prompt.Loaded += (s, ev) => textBox.Focus();

            if (prompt.ShowDialog() == true && !string.IsNullOrWhiteSpace(newCategory))
            {
                if (cmbCategory.Items.Contains(newCategory))
                {
                    MessageBox.Show($"Category '{newCategory}' มีอยู่แล้ว", "ข้อผิดพลาด", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        string dummyProductName = $"_CATEGORY_DUMMY_{Guid.NewGuid()}";
                        string insertQuery = "INSERT INTO Products (ProductName, Category, Price, Stock) VALUES (@n, @c, @p, @s)";
                        SqlCommand cmd = new SqlCommand(insertQuery, conn);

                        cmd.Parameters.AddWithValue("@n", dummyProductName);
                        cmd.Parameters.AddWithValue("@c", newCategory);
                        cmd.Parameters.AddWithValue("@p", 0);
                        cmd.Parameters.AddWithValue("@s", 0);

                        cmd.ExecuteNonQuery();
                    }

                    LoadCategories();
                    cmbCategory.SelectedItem = newCategory;
                    MessageBox.Show($"เพิ่ม Category '{newCategory}' เรียบร้อยแล้ว", "สำเร็จ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("เกิดข้อผิดพลาดในการบันทึก Category ใหม่:\n" + ex.Message, "ข้อผิดพลาด", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(e.Text, out _);
        }

        private void TxtPrice_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtPrice.Text, out decimal value))
                txtPrice.Text = value.ToString("N2");
        }

        private void TxtStock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtStock.Text, out int value))
                txtStock.Text = value.ToString("N0");
        }
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

        private void BtnAdminMenu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUserRole))
            {
                MessageBox.Show("ไม่พบข้อมูลบทบาทผู้ใช้ (Role)");
                return;
            }

            if (currentUserRole != "Admin")
            {
                MessageBox.Show("คุณไม่มีสิทธิ์เข้าถึงหน้านี้", "Access Denied",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AdminSubMenuContainer != null)
            {
                AdminSubMenuContainer.Visibility =
                    (AdminSubMenuContainer.Visibility == Visibility.Visible)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            string name = txtProductName.Text.Trim();
            string category = cmbCategory.Text.Trim();

            if (!decimal.TryParse(txtPrice.Text.Replace(",", ""), out decimal price) ||
                !int.TryParse(txtStock.Text.Replace(",", ""), out int stock))
            {
                MessageBox.Show("กรุณากรอกราคาและจำนวนสินค้าเป็นตัวเลขเท่านั้น");
                return;
            }


            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string insertQuery = "INSERT INTO Products (ProductName, Category, Price, Stock, ImagePath) VALUES (@n, @c, @p, @s, @img)";
                SqlCommand cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@img", (object)selectedImagePath ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("เพิ่มสินค้าเรียบร้อยแล้ว");
            LoadProducts();
            LoadCategories();
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is Product selected)
            {
                if (MessageBox.Show($"คุณต้องการแก้ไขสินค้า '{selected.ProductName}' หรือไม่?", "ยืนยัน", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    txtProductName.Text = selected.ProductName;
                    cmbCategory.Text = selected.Category;
                    txtPrice.Text = selected.Price.ToString();
                    txtStock.Text = selected.Stock.ToString();
                    if (!string.IsNullOrEmpty(selected.ImagePath) && File.Exists(selected.ImagePath))
                    {
                        imgPreview.Source = new BitmapImage(new Uri(selected.ImagePath, UriKind.Absolute));
                        selectedImagePath = selected.ImagePath; 
                    }
                    else
                    {
                        imgPreview.Source = null;
                        selectedImagePath = null;
                    }
                    MessageBox.Show("คุณสามารถแก้ไขข้อมูลได้ที่ฟอร์มด้านบน แล้วกด 'บันทึกการแก้ไข'");
                }
            }
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is Product selected)
            {
                if (MessageBox.Show($"คุณต้องการลบสินค้า '{selected.ProductName}' จริงหรือไม่?", "ยืนยันการลบ", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string deleteQuery = "DELETE FROM Products WHERE Id = @id";
                        SqlCommand cmd = new SqlCommand(deleteQuery, conn);
                        cmd.Parameters.AddWithValue("@id", selected.Id);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("ลบสินค้าเรียบร้อยแล้ว");
                    LoadProducts();
                }
            }
        }
        private void BtnSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is Product selected)
            {
                if (MessageBox.Show("ยืนยันการแก้ไขข้อมูลสินค้านี้หรือไม่?",
                    "ยืนยัน", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string updateQuery = @"UPDATE Products 
                                             SET ProductName=@n, 
                                             Category=@c, 
                                             Price=@p, 
                                             Stock=@s, 
                                             ImagePath=@img
                                             WHERE Id=@id";

                        SqlCommand cmd = new SqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@n", txtProductName.Text.Trim());
                        cmd.Parameters.AddWithValue("@c", cmbCategory.Text.Trim());
                        cmd.Parameters.AddWithValue("@p", decimal.Parse(txtPrice.Text));
                        cmd.Parameters.AddWithValue("@s", int.Parse(txtStock.Text));
                        cmd.Parameters.AddWithValue("@img", (object)selectedImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", selected.Id);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("บันทึกการแก้ไขเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadProducts();
                }
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
        private void BtnAboutus_Click(object sender, RoutedEventArgs e)
        {
            AboutusWindow aboutusWindow = new AboutusWindow(currentUserRole, currentUserId, currentUserName);
            aboutusWindow.Show();
            this.Close();
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            Cart cart = new Cart();
            cart.Show();
            this.Close();
        }

        private void BtnOrganizer_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ขณะนี้คุณอยู่ที่ 'จัดการสินค้า'");
            return;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
       
        private void Bordor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }
        private void Bordor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
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

    }
}
