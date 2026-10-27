using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Helpers;
using WpfApp1.Statics;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Чудо_обувьEntities _db = new Чудо_обувьEntities();
        private MessageHelper _mh = new MessageHelper();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginEnter.Text;
            var user = _db.users.Where(x => x.login == login).FirstOrDefault();

            if (user == null)
            {
                _mh.ShowError("Введен не правильный логин!");
                return;
            }

            // Сохраняем пользователя в сессию
            CurrentSession.CurrentUser = user;

            // Получаем роль пользователя из базы данных (по его role_id)
            var role = _db.roles.FirstOrDefault(r => r.role_id == user.role_id);
            string roleName = role?.name ?? "";

            // Разделяем потоки в зависимости от роли
            if (roleName == "Администратор")
            {
                new OrderWindow(true).Show(); // true = права администратора
            }
            else if (roleName == "Менеджер")
            {
                new OrderWindow(false).Show(); // false = права менеджера (без редактирования даты)
            }
            else
            {
                new ProductWindow().Show(); // Обычный клиент
            }

            Close(); // Закрываем окно авторизации
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            new ProductWindow().Show();
            Close();
        }
    }
}
