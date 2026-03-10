using System.Diagnostics;
using System.Windows;
using TankiLauncher.Views;

namespace TankiLauncher
{
    public partial class MainWindow : Window
    {
        public string? CurrentUser;

        public MainWindow()
        {
            InitializeComponent();

            // при запуске открывается авторизация
            ContentArea.Children.Add(new LoginView());
        }

        public void OnLoginSuccess(string username)
        {
            CurrentUser = username;

            PlayBtn.Visibility = Visibility.Visible;
            ProfileBtn.Visibility = Visibility.Visible;

            LoginBtn.Visibility = Visibility.Collapsed;
            RegisterBtn.Visibility = Visibility.Collapsed;

            ProfileBtn.Content = $"Профиль ({username})";

            // 🔴 ВАЖНО — сразу открыть профиль
            OpenProfile();
        }

        public void OpenProfile()
        {
            if (App.CurrentUser == null)
                return;

            ContentArea.Children.Clear();
            ContentArea.Children.Add(new ProfileView());
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Children.Clear();
            ContentArea.Children.Add(new GameView());
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Children.Clear();
            ContentArea.Children.Add(new LoginView());
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Children.Clear();
            ContentArea.Children.Add(new RegisterView());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Настройки пока не реализованы");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            OpenProfile();
        }
    }
}


