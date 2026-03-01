using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EZTicketProject
{
    public partial class MainWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void BtnForgetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("คุณต้องการรีเซ็ตรหัสผ่านใช่หรือไม่?", "ยืนยัน", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Forgetpass forgetpass = new Forgetpass();
                forgetpass.Show();
                this.Close();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string createTable = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
                        CREATE TABLE users (
                            id INT IDENTITY(1,1) PRIMARY KEY,
                            name NVARCHAR(100),
                            age INT,
                            sex NVARCHAR(10),
                            phone NVARCHAR(20) UNIQUE,
                            password NVARCHAR(100),
                            role NVARCHAR(20),
                            imagePath NVARCHAR(MAX) NULL
                        )";

                    SqlCommand cmd = new SqlCommand(createTable, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB Error: " + ex.Message);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPhone.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("กรุณากรอกเบอร์โทรและรหัสผ่าน");
                return;
            }

            string hashedPassword = HashPassword(txtPassword.Password);

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"SELECT id, name, role, phone, imagePath 
                             FROM users 
                             WHERE phone=@phone AND password=@password";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        int userId = Convert.ToInt32(reader["id"]);
                        string name = reader["name"].ToString();
                        string role = reader["role"].ToString();
                        string phone = reader["phone"].ToString();
                        string imagePath = reader["imagePath"] == DBNull.Value ? null : reader["imagePath"].ToString();

                        UserSession.SetSession(userId, name, phone, role, imagePath);

                        MessageBox.Show($"ยินดีต้อนรับ {name}!\nสถานะ: {role}");

                        ProgramWindow programWindow = new ProgramWindow();

                        programWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("เบอร์โทรหรือรหัสผ่านไม่ถูกต้อง");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message);
            }
        }

        private string HashPassword(string password)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }


        private void TxtPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            phPhone.Visibility = string.IsNullOrWhiteSpace(txtPhone.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            phPassword.Visibility = string.IsNullOrWhiteSpace(txtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }

        private void phPhone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPhone.Focus();
        }

        private void phPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword.Focus();
        }
    }
}
