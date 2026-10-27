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
    /// Логика взаимодействия для AddSizeToOrderWindow.xaml
    /// </summary>
    public partial class AddSizeToOrderWindow : Window
    {
        private Чудо_обувьEntities _db;
        private ProductViewModel _product;
        private int _orderId;

        // Свойство, чтобы вернуть ID созданного заказа (если заказ новый)
        public int CreatedOrderId { get; set; } = -1;

        // Вспомогательный класс для размеров
        public class SizeItem
        {
            public int StockItemId { get; set; }
            public decimal SizeValue { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; } // Цена на момент добавления
        }

        public AddSizeToOrderWindow(Чудо_обувьEntities db, ProductViewModel product, int orderId)
        {
            InitializeComponent();
            _db = db;
            _product = product;
            _orderId = orderId;

            ProductNameText.Text = product.name;
            LoadSizes();
        }

        private void LoadSizes()
        {
            var sizes = _db.stock_items
                .Where(si => si.product_id == _product.product_id && si.quantitty > 0)
                .Select(si => new SizeItem
                {
                    StockItemId = si.stock_item_id,
                    SizeValue = si.sizes.size_value,
                    Quantity = si.quantitty,
                    Price = _product.FinalPrice // Цена со скидкой
                })
                .ToList();

            SizeComboBox.ItemsSource = sizes;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (SizeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите корректное количество (целое число > 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedSize = SizeComboBox.SelectedItem as SizeItem;
            if (qty > selectedSize.Quantity)
            {
                MessageBox.Show($"На складе только {selectedSize.Quantity} шт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Если заказа еще нет, создаем его
                if (_orderId == -1)
                {
                    var newOrder = new orders
                    {
                        order_date = DateTime.Now,
                        user_id = WpfApp1.Statics.CurrentSession.CurrentUser.user_id,  // Если заказ создает менеджер, user_id может быть null или ID клиента
                        total_sum = 0
                    };
                    _db.orders.Add(newOrder);
                    _db.SaveChanges(); // Сохраняем, чтобы получить order_id

                    _orderId = newOrder.order_id;
                    CreatedOrderId = _orderId;
                }

                // 2. Добавляем позицию в заказ
                var orderItem = new order_items
                {
                    order_id = _orderId,
                    stock_item_id = selectedSize.StockItemId,
                    quantity = qty
                };
                _db.order_items.Add(orderItem);

                // 3. Уменьшаем остаток на складе
                var stock = _db.stock_items.First(si => si.stock_item_id == selectedSize.StockItemId);
                stock.quantitty -= qty;

                // 4. Пересчитываем итоговую сумму заказа
                var orderToUpdate = _db.orders.First(o => o.order_id == _orderId);
                // Сумма = все старые позиции + новая
                var allItems = _db.order_items.Where(oi => oi.order_id == _orderId).ToList();
                // Внимание: здесь мы считаем сумму по базовой цене. Если нужна цена со скидкой, нужно вызывать логику из ProductViewModel
                orderToUpdate.total_sum = allItems.Sum(oi => oi.quantity * oi.stock_items.products.price) + (qty * selectedSize.Price);

                _db.SaveChanges();

                MessageBox.Show("Товар добавлен в заказ.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
