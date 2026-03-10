using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TankiLauncher.Views
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();

            Loaded += ProfileView_Loaded;
        }

        private async void ProfileView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await LoadProfile();
        }

        private async Task LoadProfile()
        {
            try
            {
                string username = App.CurrentUser ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username))
                    return;

                HttpClient client = new HttpClient();

                var response = await client.GetStringAsync($"http://localhost:5000/profile/{username}");

                var profile = JsonSerializer.Deserialize<PlayerProfile>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (profile == null)
                    return;

                // ===== ЗАПОЛНЯЕМ UI =====

                UsernameText.Text = profile.Username;
                RankText.Text = $"Ранг: {profile.Rank}";

                ExpBar.Maximum = profile.NextLevelExp;
                ExpBar.Value = profile.Experience;

                ExpText.Text = $"{profile.Experience} / {profile.NextLevelExp} XP";

                SilverText.Text = profile.Silver.ToString();
                GoldText.Text = profile.Gold.ToString();
                IronText.Text = profile.Iron.ToString();
                CopperText.Text = profile.Copper.ToString();
                TitaniumText.Text = profile.Titanium.ToString();
                FuelText.Text = profile.Fuel.ToString();

                BattlesText.Text = profile.Battles.ToString();
                WinsText.Text = profile.Wins.ToString();
                KDText.Text = profile.KD.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class PlayerProfile
    {
        public string Username { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public int NextLevelExp { get; set; }

        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Iron { get; set; }
        public int Copper { get; set; }
        public int Titanium { get; set; }
        public int Fuel { get; set; }

        public int Battles { get; set; }
        public int Wins { get; set; }
        public int KD { get; set; }

        public string Rank { get; set; } = string.Empty;
    }
}