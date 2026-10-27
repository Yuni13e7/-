using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для OrderEditWindow.xaml
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();
        private int _orderId;
        private bool _isAdmin;

        public OrderEditWindow(int orderId, bool isAdmin)
        {
            InitializeComponent();
            _orderId = orderId;
            _isAdmin = isAdmin;

            if (_isAdmin)
            {
                SaveDateBtn.Visibility = Visibility.Visible;
            }

            LoadOrderData();
        }

        private void LoadOrderData()
        {
            var order = _db.orders.FirstOrDefault(o => o.order_id == _orderId);
            if (order != null)
            {
                OrderDatePicker.SelectedDate = order.order_date;
            }

            // Загружаем позиции
            var items = _db.order_items
                .Where(oi => oi.order_id == _orderId)
                .Select(oi => new OrderItemViewModel
                {
                    OrderItemId = oi.order_item_id,
                    ProductName = oi.stock_items.products.name,
                    ManufacturerName = oi.stock_items.products.manufacturers.name,
                    SizeValue = oi.stock_items.sizes.size_value,
                    Quantity = oi.quantity,
                    PricePerUnit = oi.stock_items.products.price // или со скидкой, если нужно
                })
                .ToList();

            ItemsGrid.ItemsSource = items;
            TotalText.Text = $"Итого: {items.Sum(i => i.Total):N2} ₽";
        }

        private void SaveDate_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDatePicker.SelectedDate == null) return;

            var order = _db.orders.FirstOrDefault(o => o.order_id == _orderId);
            if (order != null)
            {
                order.order_date = OrderDatePicker.SelectedDate.Value;
                _db.SaveChanges();
                MessageBox.Show("Дата изменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrderData();
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            // Открываем каталог для выбора товара
            var selectWindow = new ProductSelectionWindow(_db, _isAdmin, _orderId);
            selectWindow.ShowDialog();
            LoadOrderData(); // Обновляем список позиций
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is OrderItemViewModel selectedItem)
            {
                var result = MessageBox.Show("Удалить эту позицию из заказа?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var item = _db.order_items.FirstOrDefault(oi => oi.order_item_id == selectedItem.OrderItemId);
                    if (item != null)
                    {
                        // Возвращаем товар на склад
                        var stock = _db.stock_items.FirstOrDefault(s => s.stock_item_id == item.stock_item_id);
                        if (stock != null) stock.quantitty += item.quantity;

                        _db.order_items.Remove(item);

                        // Пересчитываем сумму заказа
                        var order = _db.orders.First(o => o.order_id == _orderId);
                        var remaining = _db.order_items.Where(oi => oi.order_id == _orderId && oi.order_item_id != item.order_item_id);
                        order.total_sum = remaining.Sum(oi => oi.quantity * oi.stock_items.products.price);

                        _db.SaveChanges();
                        LoadOrderData();
                    }
                }
            }
        }
    }
}
