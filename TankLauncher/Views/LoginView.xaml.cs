using System.Windows;
using System.Windows.Controls;
using TankiLauncher.Services;

namespace TankiLauncher.Views
{
    public partial class LoginView : UserControl
    {
        private AuthService auth = new AuthService();

        public LoginView()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            bool success = await auth.Login(username, password);

            if (success)
            {
                App.CurrentUser = username;
                MessageBox.Show("Вход выполнен");

                var main = (MainWindow)Application.Current.MainWindow;

                if (main != null)
                    main.OnLoginSuccess(username);
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль");
            }
        }
    }
}