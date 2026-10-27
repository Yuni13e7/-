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
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();
        private bool _isAdmin = false;
        public OrderWindow(bool isAdmin)
        {
            InitializeComponent();
            _isAdmin = isAdmin;

            // Показываем кнопку редактирования только админу
            if (_isAdmin)
            {
                EditOrderBtn.Visibility = Visibility.Visible;
            }

            LoadOrders();
        }

        public void LoadOrders()
        {
            // Загружаем заказы вместе с пользователями
            var orders = _db.orders
    .OrderByDescending(o => o.order_date)
    // 1. Сначала выгружаем "сырые" данные из БД в память
    .Select(o => new
    {
        o.order_id,
        o.order_date,
        o.total_sum,
        // Проверяем, есть ли пользователь, и забираем его поля
        LastName = o.users != null ? o.users.last_name : null,
        FirstName = o.users != null ? o.users.first_name : null
    })
    .ToList() // <-- ВАЖНО: ToList() здесь останавливает запрос к БД
              // 2. Теперь работаем с данными в памяти, где C# может делать что угодно
    .Select(o => new OrderViewModel
    {
        OrderId = o.order_id,
        OrderDate = o.order_date,
        // Теперь интерполяция строк работает
        ClientFullName = o.LastName != null ? $"{o.LastName} {o.FirstName}" : "Неизвестно",
        TotalSum = o.total_sum
    })
    .ToList();

            OrdersGrid.ItemsSource = orders;
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            // Открываем каталог для выбора товара (с поиском)
            var selectWindow = new ProductSelectionWindow(_db, _isAdmin);
            if (selectWindow.ShowDialog() == true)
            {
                // Если пользователь сформировал заказ внутри окна выбора
                LoadOrders();
            }
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is OrderViewModel selectedOrder)
            {
                var result = MessageBox.Show($"Удалить заказ №{selectedOrder.OrderId}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var order = _db.orders.FirstOrDefault(o => o.order_id == selectedOrder.OrderId);
                    if (order != null)
                    {
                        // Удаляем сначала позиции заказа, потом сам заказ
                        var items = _db.order_items.Where(oi => oi.order_id == order.order_id).ToList();
                        _db.order_items.RemoveRange(items);
                        _db.orders.Remove(order);
                        _db.SaveChanges();
                        LoadOrders();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is OrderViewModel selectedOrder)
            {
                // Открываем окно редактирования
                var editWindow = new OrderEditWindow(selectedOrder.OrderId, _isAdmin);
                editWindow.ShowDialog();
                LoadOrders(); // Обновляем после закрытия
            }
            else
            {
                MessageBox.Show("Выберите заказ для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OrdersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Двойной клик по заказу открывает просмотр состава
            if (OrdersGrid.SelectedItem is OrderViewModel selectedOrder)
            {
                var editWindow = new OrderEditWindow(selectedOrder.OrderId, _isAdmin);
                editWindow.ShowDialog();
                LoadOrders();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }
    }
}
