using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font;
using Path = System.IO.Path;

namespace WpfApp1
{
    public partial class AnalyticsWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();

        public class TopProductViewModel
        {
            public string ProductName { get; set; }
            public string ManufacturerName { get; set; }
            public int TotalQuantity { get; set; }
        }

        public AnalyticsWindow()
        {
            InitializeComponent();

            DateTime now = DateTime.Now;
            StartDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
            EndDatePicker.SelectedDate = now;
        }

        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            CalculateAnalytics();
        }

        private void CalculateAnalytics()
        {
            // 1. Валидация дат
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
                return;

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1); // Конец дня

            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ordersInPeriod = _db.orders
                .Where(o => o.order_date >= start && o.order_date <= end)
                .ToList();

            TotalOrdersText.Text = ordersInPeriod.Count.ToString();
            TotalSumText.Text = $"{ordersInPeriod.Sum(o => o.total_sum):N2} ₽";

            var orderIds = ordersInPeriod.Select(o => o.order_id).ToList();

            var topProducts = _db.order_items
                .Where(oi => orderIds.Contains(oi.order_id))
                .GroupBy(oi => new
                {
                    ProductName = oi.stock_items.products.name,
                    ManufacturerName = oi.stock_items.products.manufacturers.name
                })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    ManufacturerName = g.Key.ManufacturerName,
                    TotalQuantity = g.Sum(oi => oi.quantity)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(5)
                .ToList();

            TopProductsGrid.ItemsSource = topProducts;
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите даты для формирования отчета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Отчет_Аналитика_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf");

                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document document = new Document(pdf))
                {
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    string boldFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arialbd.ttf");

                    PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
                    PdfFont boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H);

                    Paragraph title = new Paragraph("Аналитический отчет")
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                    document.Add(title);

                    document.Add(new Paragraph($"Период: с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}")
                        .SetFont(font).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    document.Add(new Paragraph($"\nВсего заказов: {TotalOrdersText.Text}")
                        .SetFont(font).SetFontSize(14));

                    document.Add(new Paragraph($"Общая сумма продаж: {TotalSumText.Text}")
                        .SetFont(font).SetFontSize(14));

                    document.Add(new Paragraph("\nТоп-5 моделей по продажам:").SetFont(boldFont).SetFontSize(16));

                    Table table = new Table(3).UseAllAvailableWidth();
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Модель").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Производитель").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Кол-во пар").SetFont(boldFont)));

                    var top5 = TopProductsGrid.ItemsSource as List<TopProductViewModel>;
                    if (top5 != null)
                    {
                        foreach (var item in top5)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(item.ProductName).SetFont(font)));
                            table.AddCell(new Cell().Add(new Paragraph(item.ManufacturerName).SetFont(font)));
                            table.AddCell(new Cell().Add(new Paragraph(item.TotalQuantity.ToString()).SetFont(font)));
                        }
                    }
                    document.Add(table);
                }

                MessageBox.Show($"Отчет успешно сохранен!\nПуть: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new OrderWindow(true).Show();
            Close();
        }
    }
}