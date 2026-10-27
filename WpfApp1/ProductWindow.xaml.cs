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
    /// Логика взаимодействия для ProductWindow.xaml
    /// </summary>
    public partial class ProductWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();
        private List<ProductViewModel> _productViewModels = new List<ProductViewModel>();   
        private List<int> _soldProductIds = new List<int>();

        public ProductWindow()
        {
            InitializeComponent();
            LoadProducts();
            LoadData();
            LoadUI();
        }
        private void LoadUI()
        {
            users user = CurrentSession.CurrentUser;
            if (user.role_id == 1 || user.role_id == 2)
            {
                AdminPanel.Visibility = Visibility.Collapsed;
            }

        }
        private void LoadData()
        {
            SortingComboBox.SelectedIndex = 0;
            FilteringComboBox.SelectedIndex = 0;
            users user = CurrentSession.CurrentUser;
            if (user != null)
                FullUserName.Text = $"{user.last_name} {user.first_name} {user.middle_name}"; 
        }

        private void LoadProducts()
        {
            // вычисляем границы прошлого месяца для расчета скидки
            DateTime firstDay  = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
            //получаем список товаров на которые были заказы в прошлом месяце 
            _soldProductIds = _db.order_items
                .Where(oi => oi.orders.order_date >= firstDay && oi.orders.order_date <= lastDay)
                .Select(oi => oi.stock_items.product_id)
                .Distinct()
                .ToList();
            // загружаем все товары
            var products = _db.products.ToList();
            // создаем viewModel для каждого товара 
            foreach(var p in products)
            {
                //проверяем есть ли айди в списке проданных
                bool hasOrders = _soldProductIds.Contains(p.product_id);
                // передаем два параметра товар и флаг заказов
                _productViewModels.Add(new ProductViewModel(p, hasOrders));
            }
            ProductList.ItemsSource = _productViewModels;
        }
        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }
        private void ApplyFilter()
        {
            var result = _productViewModels.AsEnumerable();

            // 1. ПОИСК по названию и описанию
            string searchText = (SearchingTextBox.Text ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                result = result.Where(p =>
                    (p.name ?? "").ToLower().Contains(searchText) ||
                    (p.description ?? "").ToLower().Contains(searchText));
            }

            // 2. СОРТИРОВКА по цене
            if (SortingComboBox.SelectedItem is ComboBoxItem sortItem)
            {
                string sort = sortItem.Content?.ToString();
                if (sort == "По возрастанию")
                    result = result.OrderBy(p => p.FinalPrice);
                else if (sort == "По убыванию")
                    result = result.OrderByDescending(p => p.FinalPrice);
            }

            // 3. ФИЛЬТРАЦИЯ по категории
            if (FilteringComboBox.SelectedItem is ComboBoxItem filterItem)
            {
                string category = filterItem.Content?.ToString();
                if (category != "Все категории")
                {
                    result = result.Where(p =>
                        p.categories != null && p.categories.name == category);
                }
            }
            ProductList.ItemsSource = result.ToList();
        }

        private void SearchingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilter();
        }

        private void SortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilter();
        }

        private void FilteringComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilter();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Получаем карточку и товар, на который кликнули
            var border = sender as Border;
            if (border == null) return;

            var product = border.DataContext as ProductViewModel;
            if (product == null) return;

            // Открываем окно деталей
            var detailWindow = new ProductDetailWindow(product);
            detailWindow.ShowDialog(); // модально — пока не закроют, назад не вернуться

            // После закрытия — обновляем каталог (кол-во могло измениться)
            if (detailWindow.OrderChanged)
            {
                _productViewModels.Clear();
                _soldProductIds.Clear();
                LoadProducts();
            }
        }
    }
}
