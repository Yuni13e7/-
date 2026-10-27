using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1.Helpers
{
    public class MessageHelper
    {
        public void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
