using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp1.ViewModels
{
    public class ProductViewModel
    {
        public int TotalStockQuantity { get; set; }

        public string StockQuantityTotal { get; set; }

        public bool IsDiscounted { get; set; }
        public ProductViewModel(products product, bool hasOrders)
        {
            product_id = product.product_id;
            name =product.name;
            image =product.image;
            description =product.description;
            composition =product.composition;
            price = product.price;

            categories =product.categories;
            manufacturers =product.manufacturers;
            subcategories  =product.subcategories;
            TotalStockQuantity = product.TotalStockQuantity;

            if (TotalStockQuantity > 5)
            {
                StockQuantityTotal = "Много";
            }
            else
                StockQuantityTotal = "Мало";


            IsDiscounted = !hasOrders; // Если заказов НЕТ, то скидка ЕСТЬ
            GetPrice(); // Считаем цену

            GetBackground();
            GetPhoto();
            

        }

        public int product_id { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string description { get; set; }
        public string composition { get; set; }
        public decimal price { get; set; }
        public decimal OldPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public categories categories { get; set; }
        public manufacturers manufacturers { get; set; }
        public subcategories subcategories { get; set; }
        

        public Brush Background { get; set; }
        private void GetBackground()
        {
            if (TotalStockQuantity <= 3)
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FF8080");
                return;
            }
        }
        private void GetPhoto()
        {
            if (image != "")
                return;
            image = "/Res/picture.png";
        }
        private void GetPrice()
        {
            if (IsDiscounted)
            {
                OldPrice = price;              // Сохраняем старую
                FinalPrice = price * 0.75m;    // Считаем новую
            }
            else
            {
                OldPrice = 0;
                FinalPrice = price;            // Цена без изменений
            }
        }
        public System.Windows.Visibility OldPriceVisibility
        {
            get
            {
                if (IsDiscounted == true)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
        }
    }
}
