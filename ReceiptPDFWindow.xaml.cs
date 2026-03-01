using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EZTicketProject
{
    public partial class ReceiptPDFWindow : Window
    {
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ETicketDB;Integrated Security=True";

        public ReceiptViewModel ViewModel { get; set; }

        public ReceiptPDFWindow(int orderId)
        {
            InitializeComponent();

            ViewModel = LoadReceiptData(orderId);
            DataContext = ViewModel;
        }

        private ReceiptViewModel LoadReceiptData(int orderId)
        {
            ReceiptViewModel vm = new ReceiptViewModel
            {
                OrderId = orderId,
                OrderCode = "0132" + orderId,
                OrderDate = DateTime.Now.ToString("dd/MM/yyyy"),
            };

            vm.Items = LoadOrderItems(orderId);
            vm.FinalTotal = vm.Items.Sum(i => i.Price * i.Quantity);
            vm.PriceBeforeVat = vm.FinalTotal / 1.07m;
            vm.Vat = vm.FinalTotal - vm.PriceBeforeVat;

            return vm;
        }

        private List<OrderItemModel> LoadOrderItems(int orderId)
        {
            var list = new List<OrderItemModel>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = "SELECT ProductName, Quantity, Price FROM OrderItems WHERE OrderId=@id";

                var cmd = new SqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@id", orderId);

                var r = cmd.ExecuteReader();

                while (r.Read())
                {
                    list.Add(new OrderItemModel
                    {
                        ProductName = r["ProductName"].ToString(),
                        Quantity = Convert.ToInt32(r["Quantity"]),
                        Price = Convert.ToDecimal(r["Price"])
                    });
                }
            }

            return list;
        }

        private void BtnSavePDF_Click(object sender, RoutedEventArgs e)
        {
            string downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            Directory.CreateDirectory(downloads);

            string filePath = Path.Combine(downloads, $"Receipt_{ViewModel.OrderId}.pdf");

            GeneratePDF(filePath);

            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
            }

            MessageBox.Show($"PDF ถูกบันทึกแล้ว:\n{filePath}");

            Profile p = new Profile();
            p.Show();
            this.Close();
        }


        private void GeneratePDF(string filePath)
        {
            try
            {

                string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts/DB Helvethaica X Li.ttf");
                BaseFont thaiFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font fontNormal = new Font(thaiFont, 16);
                Font fontBold = new Font(thaiFont, 18, Font.BOLD);

                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                doc.Open();

                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images/Profile.png");
                if (File.Exists(logoPath))
                {
                    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                    logo.ScaleAbsolute(90, 90);
                    logo.Alignment = Element.ALIGN_CENTER;
                    doc.Add(logo);
                }

                Paragraph shop = new Paragraph("EZDrink", new Font(thaiFont, 26, Font.BOLD));
                shop.Alignment = Element.ALIGN_CENTER;
                doc.Add(shop);

                doc.Add(new Paragraph(" "));

                doc.Add(new Paragraph("บริษัทอีซี่ดริ้ง จำกัด (สำนักงานใหญ่)", fontBold));
                doc.Add(new Paragraph("123 หมู่ 16 ถนนมิตรภาพ ตำบลในเมือง อำเภอเมือง จังหวัดขอนแก่น 40002", fontNormal));
                doc.Add(new Paragraph("เลขประจำตัวผู้เสียภาษี: 1122334455667", fontNormal));
                doc.Add(new Paragraph("โทร. 0555555555", fontNormal));
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph($"เลขที่ออเดอร์: {ViewModel.OrderCode}", fontBold));
                doc.Add(new Paragraph($"วันที่สั่งซื้อ: {ViewModel.OrderDate}", fontNormal));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2.5f, 1f, 1f });

                PdfPCell Header(string text)
                {
                    return new PdfPCell(new Phrase(text, fontBold))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = new BaseColor(230, 230, 230),
                        Padding = 5
                    };
                }

                table.AddCell(Header("รายการสินค้า"));
                table.AddCell(Header("จำนวน"));
                table.AddCell(Header("ราคา (บาท)"));

                foreach (var item in ViewModel.Items)
                {
                    table.AddCell(new Phrase(item.ProductName, fontNormal));
                    table.AddCell(new Phrase(item.Quantity.ToString(), fontNormal) { });
                    table.AddCell(new Phrase($"{item.Price * item.Quantity:N2}", fontNormal));
                }

                doc.Add(table);
                doc.Add(new Paragraph(" "));

                Paragraph p1 = new Paragraph($"ราคาสินค้า: {ViewModel.PriceBeforeVat:N2} บาท", fontNormal);
                p1.Alignment = Element.ALIGN_RIGHT;
                doc.Add(p1);

                Paragraph p2 = new Paragraph($"VAT 7%: {ViewModel.Vat:N2} บาท", fontNormal);
                p2.Alignment = Element.ALIGN_RIGHT;
                doc.Add(p2);

                Paragraph p3 = new Paragraph($"ยอดชำระทั้งหมด: {ViewModel.FinalTotal:N2} บาท", fontBold);
                p3.Alignment = Element.ALIGN_RIGHT;
                doc.Add(p3);

                doc.Add(new Paragraph(" "));

                Paragraph status = new Paragraph("สถานะสินค้า: ชำระแล้ว (กำลังดำเนินการจัดส่งสินค้า)", fontBold);
                doc.Add(new Paragraph($"วันที่ออกใบเสร็จ: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontBold));
                status.Alignment = Element.ALIGN_LEFT;
                doc.Add(status);

                doc.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message);
            }
        }



        private void AddHeader(PdfPTable t, string text)
        {
            PdfPCell c = new PdfPCell(new Phrase(text, FontFactory.GetFont("Arial", 12, Font.BOLD)));
            c.HorizontalAlignment = Element.ALIGN_CENTER;
            c.BackgroundColor = new BaseColor(240, 240, 240);
            t.AddCell(c);
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Profile profile = new Profile();
            profile.Show();
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

    public class ReceiptViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string OrderDate { get; set; }

        public decimal PriceBeforeVat { get; set; }
        public decimal Vat { get; set; }
        public decimal FinalTotal { get; set; }

        public List<OrderItemModel> Items { get; set; }
    }

    public class OrderItemModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
