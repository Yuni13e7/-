using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModels
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string ClientFullName { get; set; }
        public decimal TotalSum { get; set; }
    }

    // Класс для состава заказа
    public class OrderItemViewModel
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string ManufacturerName { get; set; }
        public decimal SizeValue { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Total => Quantity * PricePerUnit;
    }
}
