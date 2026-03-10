using System.Windows;
using System.Windows.Controls;
using TankiLauncher.Services;

namespace TankiLauncher.Views
{
    public partial class RegisterView : UserControl
    {
        AuthService auth = new AuthService();

        public RegisterView()
        {
            InitializeComponent();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            bool success = await auth.Register(username, password);

            if (success)
                MessageBox.Show("Аккаунт создан");
            else
                MessageBox.Show("Ошибка регистрации");
        }
    }
}