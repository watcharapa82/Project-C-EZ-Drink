using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace EZTicketProject
{
    public partial class Forgetpass : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        public Forgetpass()
        {
            InitializeComponent();
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void btnBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow mainwindow = new MainWindow();
            mainwindow.Show();
            this.Close();
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string phone = txtPhone.Text.Trim();
            string newPassword = txtNewPass.Password.Trim();
            string confirmPassword = txtConfirmPass.Password.Trim();

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("กรุณากรอกเบอร์โทรศัพท์");
                return;
            }
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("กรุณากรอกรหัสผ่านใหม่");
                return;
            }
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("รหัสผ่านไม่ตรงกัน");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string check = "SELECT id FROM users WHERE phone = @phone";
                SqlCommand checkCmd = new SqlCommand(check, conn);
                checkCmd.Parameters.AddWithValue("@phone", phone);

                object userObj = checkCmd.ExecuteScalar();

                if (userObj == null)
                {
                    MessageBox.Show("ไม่พบผู้ใช้จากเบอร์โทรนี้");
                    return;
                }

                int userId = Convert.ToInt32(userObj);

                string hashed = HashPassword(newPassword);

                string update = "UPDATE users SET password = @pass WHERE id = @id";
                SqlCommand updateCmd = new SqlCommand(update, conn);
                updateCmd.Parameters.AddWithValue("@pass", hashed);
                updateCmd.Parameters.AddWithValue("@id", userId);

                updateCmd.ExecuteNonQuery();

                MessageBox.Show("เปลี่ยนรหัสผ่านสำเร็จแล้ว!");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
