using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;


namespace EZTicketProject
{
    public partial class RegisterWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";
        string selectedRole = "";
        private readonly string currentUserRole;
        private readonly int currentUserId;
        private readonly string currentUserName;
        private byte[] selectedImageBytes = null;
        private string tempSelectedImagePath;

        public RegisterWindow()
        {
            InitializeComponent();
            this.currentUserRole = "Guest"; 
        }
        public RegisterWindow(string currentUserRole)
        {
            InitializeComponent();
            this.currentUserRole = currentUserRole;
        }
        private void ImgAdmin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedRole = "Admin";
            AnimateSelection(imgAdmin, imgUser);
        }
        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.DecodePixelWidth = 350; 
                bitmap.EndInit();
                bitmap.Freeze();

                imgPreview.Source = bitmap;
                selectedImageBytes = File.ReadAllBytes(openFileDialog.FileName);

                tempSelectedImagePath = openFileDialog.FileName;
            }
        }

        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow mainwindow = new MainWindow();
            mainwindow.Show();
            this.Close();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ImgUser_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedRole = "User";
            AnimateSelection(imgUser, imgAdmin);
        }
        private string SaveProfileImage()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserImages");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = "user_" + DateTime.Now.Ticks + Path.GetExtension(tempSelectedImagePath);
            string destPath = Path.Combine(dir, fileName);

            File.Copy(tempSelectedImagePath, destPath, true);

            return destPath; 
        }

        private void AnimateSelection(System.Windows.Controls.Image selected, System.Windows.Controls.Image other)
        {
            DoubleAnimation grow = new DoubleAnimation(120, TimeSpan.FromMilliseconds(150));
            DoubleAnimation shrink = new DoubleAnimation(90, TimeSpan.FromMilliseconds(150));

            selected.BeginAnimation(System.Windows.Controls.Image.WidthProperty, grow);
            selected.BeginAnimation(System.Windows.Controls.Image.HeightProperty, grow);

            other.BeginAnimation(System.Windows.Controls.Image.WidthProperty, shrink);
            other.BeginAnimation(System.Windows.Controls.Image.HeightProperty, shrink);

            DropShadowEffect selectedEffect = new DropShadowEffect
            {
                Color = Colors.White,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 30,
                Opacity = 1.0
            };

            DropShadowEffect otherEffect = new DropShadowEffect
            {
                Color = Colors.LightGray,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 5,
                Opacity = 0.4
            };

            selected.Effect = selectedEffect;
            other.Effect = otherEffect;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string ageText = txtAge.Text.Trim();
            string sex = txtSex.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ageText) ||
                string.IsNullOrWhiteSpace(sex) || string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("รหัสผ่านและยืนยันไม่ตรงกัน");
                return;
            }

            if (!int.TryParse(ageText, out int age) || age < 20)
            {
                MessageBox.Show("อายุไม่ถูกต้อง (ต้องมากกว่า 20 ปี)");
                return;
            }

            if (!Regex.IsMatch(phone, @"^\d{8,10}$"))
            {
                MessageBox.Show("เบอร์โทรศัพท์ไม่ถูกต้อง");
                return;
            }

            if (selectedImageBytes == null || string.IsNullOrEmpty(tempSelectedImagePath))
            {
                MessageBox.Show("กรุณาเลือกรูปภาพโปรไฟล์ก่อน");
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedRole))
            {
                MessageBox.Show("กรุณาเลือกประเภทบัญชี (Admin / User)");
                return;
            }

            try
            {
                string savedImagePath = SaveProfileImage();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string insert = @"INSERT INTO users 
                (name, age, sex, phone, password, role, imagePath) 
                VALUES 
                (@name, @age, @sex, @phone, @password, @role, @imagePath)";

                    using (var cmd = new SqlCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@age", age);
                        cmd.Parameters.AddWithValue("@sex", sex);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@role", selectedRole);
                        cmd.Parameters.AddWithValue("@imagePath", savedImagePath);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"ลงทะเบียนสำเร็จในฐานะ {selectedRole}");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    MessageBox.Show("เบอร์โทรนี้มีผู้ใช้แล้ว");

                else
                    MessageBox.Show($"ฐานข้อมูลผิดพลาด: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message);
            }
        }

    }
}