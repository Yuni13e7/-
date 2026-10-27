using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Statics
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int StockItemId { get; set; }
        public decimal SizeValue { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Total => Quantity * PricePerUnit;
    }

    // Статическая корзина — видна из любого окна
    public static class Cart
    {
        public static List<CartItem> Items { get; set; } = new List<CartItem>();

        public static void Clear()
        {
            Items.Clear();
        }
    }
}
