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
using WpfApp1.Statics;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для ProductDetailWindow.xaml
    /// </summary>
    public partial class ProductDetailWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();
        private ProductViewModel _product;
        // Флаг: были ли изменения (для обновления каталога)
        public bool OrderChanged { get; set; } = false;

        public ProductDetailWindow(ProductViewModel product)
        {
            InitializeComponent();
            _product = product;
            DataContext = product; // привязка XAML к товару
            LoadSizes();
        }

        // Загружаем доступные размеры (где quantity > 0)
        private void LoadSizes()
        {
            var availableSizes = _db.stock_items
                .Where(si => si.product_id == _product.product_id && si.quantitty > 0)
                .Select(si => new SizeItem // ← Используем наш новый класс
                {
                    StockItemId = si.stock_item_id,
                    SizeValue = si.sizes.size_value,
                    Quantity = si.quantitty
                })
                .ToList();

            SizeComboBox.ItemsSource = availableSizes;
        }

        public class SizeItem
        {
            public int StockItemId { get; set; }
            public decimal SizeValue { get; set; }
            public int Quantity { get; set; }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            // Проверка: выбран ли размер
            if (SizeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка: корректное ли количество
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите корректное количество (целое число > 0).",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Достаём выбранный размер (теперь без dynamic!)
            var selected = SizeComboBox.SelectedItem as SizeItem;
            if (selected == null) return;

            int stockItemId = selected.StockItemId;
            decimal sizeValue = selected.SizeValue;
            int available = selected.Quantity;

            // Проверка: не превышает ли количество доступное
            if (qty > available)
            {
                MessageBox.Show($"На складе только {available} шт. этого размера.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Добавляем в корзину
            Cart.Items.Add(new CartItem
            {
                ProductId = _product.product_id,
                ProductName = _product.name,
                StockItemId = stockItemId,
                SizeValue = sizeValue,
                Quantity = qty,
                PricePerUnit = _product.FinalPrice
            });

            OrderChanged = true;

            MessageBox.Show($"Товар добавлен в заказ ({qty} шт., размер {sizeValue}).",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Очищаем поля для следующего добавления
            SizeComboBox.SelectedItem = null;
            QuantityTextBox.Text = "1";
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            // Проверка: есть ли что оформлять
            if (Cart.Items.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Добавьте хотя бы один товар.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Подтверждение
            var result = MessageBox.Show(
                $"Оформить заказ на сумму {Cart.Items.Sum(i => i.Total):N2} ₽?",
                "Подтверждение заказа",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Сохраняем в БД
            var user = CurrentSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Вы не авторизованы.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Создаём заказ
            var newOrder = new orders
            {
                order_date = System.DateTime.Now,
                user_id = user.user_id,
                total_sum = Cart.Items.Sum(i => i.Total)
            };
            _db.orders.Add(newOrder);
            _db.SaveChanges(); // сохраняем, чтобы получить order_id

            // Добавляем позиции заказа
            foreach (var item in Cart.Items)
            {
                _db.order_items.Add(new order_items
                {
                    order_id = newOrder.order_id,
                    stock_item_id = item.StockItemId,
                    quantity = item.Quantity
                });

                // Уменьшаем количество на складе
                var stockItem = _db.stock_items
                    .FirstOrDefault(si => si.stock_item_id == item.StockItemId);
                if (stockItem != null)
                    stockItem.quantitty -= item.Quantity;
            }
            _db.SaveChanges();

            MessageBox.Show("Заказ успешно оформлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Cart.Clear();
            OrderChanged = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Просто закрыть окно, ничего не сохраняя
            Close();
        }
    }
}
