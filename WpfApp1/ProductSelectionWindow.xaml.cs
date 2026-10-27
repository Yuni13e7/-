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
    /// Логика взаимодействия для ProductSelectionWindow.xaml
    /// </summary>
    public partial class ProductSelectionWindow : Window
    {
        private Чудо_обувьEntities _db;
        private bool _isAdmin;
        private int _orderId;
        private List<ProductViewModel> _allProducts;

        // Конструктор для создания НОВОГО заказа
        public ProductSelectionWindow(Чудо_обувьEntities db, bool isAdmin)
        {
            InitializeComponent();
            _db = db;
            _isAdmin = isAdmin;
            _orderId = -1; // -1 означает, что заказ еще не создан

            LoadProducts();
        }

        // Конструктор для добавления в СУЩЕСТВУЮЩИЙ заказ
        public ProductSelectionWindow(Чудо_обувьEntities db, bool isAdmin, int orderId)
        {
            InitializeComponent();
            _db = db;
            _isAdmin = isAdmin;
            _orderId = orderId;

            LoadProducts();
        }

        private void LoadProducts()
        {
            var products = _db.products.ToList();
            _allProducts = products.Select(p => new ProductViewModel(p, false)).ToList();
            ProductsList.ItemsSource = _allProducts;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string search = SearchBox.Text.ToLower();
            ProductsList.ItemsSource = _allProducts.Where(p =>
                p.name.ToLower().Contains(search) ||
                p.description.ToLower().Contains(search)).ToList();
        }

        private void ProductsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductsList.SelectedItem is ProductViewModel product)
            {
                // Открываем форму выбора размера (вы можете использовать ваш ProductDetailWindow, 
                // но лучше сделать его копию, которая сохраняет не в Cart, а сразу в заказ)
                var sizeWindow = new AddSizeToOrderWindow(_db, product, _orderId);
                if (sizeWindow.ShowDialog() == true)
                {
                    // Если заказ только создавался (id == -1), получаем новый id
                    if (_orderId == -1) _orderId = sizeWindow.CreatedOrderId;

                    DialogResult = true; // Сигнал, что заказ изменен
                    Close();
                }
            }
        }
    }
}
