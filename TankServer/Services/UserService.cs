using System.Text.Json;
using TankiServer.Models;

namespace TankiServer.Services
{
    public class UserService
    {
        private static readonly Dictionary<string, string> users = new();
        private static Dictionary<string, PlayerProfile> profiles = new();
        private static readonly object sync = new();

        private static readonly string dbPath = "Data/players.json";

        static UserService()
        {
            LoadDatabase();
        }

        public static void LoadDatabase()
        {
            Console.WriteLine("[DB] Loading database...");

            if (!File.Exists(dbPath))
            {
                Console.WriteLine("[DB] players.json not found, creating new database");
                Directory.CreateDirectory("Data");
                File.WriteAllText(dbPath, "{}");
            }

            var json = File.ReadAllText(dbPath);
            profiles = JsonSerializer.Deserialize<Dictionary<string, PlayerProfile>>(json)
                ?? new Dictionary<string, PlayerProfile>();

            foreach (var p in profiles)
            {
                users[p.Key] = "password";
            }

            Console.WriteLine($"[DB] Loaded players: {profiles.Count}");
        }

        public static void SaveDatabase()
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dbPath, json);
            Console.WriteLine("[DB] Saved players");
        }

        public bool Register(string username, string password)
        {
            lock (sync)
            {
                if (profiles.ContainsKey(username))
                    return false;

                users[username] = password;
                profiles[username] = new PlayerProfile
                {
                    Username = username,
                    PlayerColorHex = GenerateStableColor(username)
                };

                SaveDatabase();
                return true;
            }
        }

        public bool Login(string username, string password)
        {
            return users.ContainsKey(username);
        }

        public PlayerProfile? GetProfile(string username)
        {
            lock (sync)
            {
                if (profiles.TryGetValue(username, out var profile))
                    return profile;

                return null;
            }
        }

        public PlayerProfile GetOrCreateProfile(string username)
        {
            lock (sync)
            {
                if (!profiles.TryGetValue(username, out var profile))
                {
                    profile = new PlayerProfile
                    {
                        Username = username,
                        PlayerColorHex = GenerateStableColor(username)
                    };

                    profiles[username] = profile;
                    SaveDatabase();
                }

                if (string.IsNullOrWhiteSpace(profile.PlayerColorHex))
                {
                    profile.PlayerColorHex = GenerateStableColor(username);
                    SaveDatabase();
                }

                return profile;
            }
        }

        public string EnsurePlayerColor(string username, string requestedColorHex)
        {
            lock (sync)
            {
                var profile = GetOrCreateProfile(username);

                if (profile.PlayerColorHex.StartsWith("#") && profile.PlayerColorHex.Length == 7)
                    return profile.PlayerColorHex;

                profile.PlayerColorHex = IsValidColor(requestedColorHex)
                    ? requestedColorHex
                    : GenerateStableColor(username);

                SaveDatabase();
                return profile.PlayerColorHex;
            }
        }

        public void ApplyResourcePickup(string username, string resourceType)
        {
            lock (sync)
            {
                var profile = GetOrCreateProfile(username);

                switch (resourceType)
                {
                    case "iron":
                        profile.Iron += 1;
                        break;
                    case "silver":
                        profile.Silver += 5;
                        break;
                    case "gold":
                        profile.Gold += 1;
                        break;
                    case "copper":
                        profile.Copper += 1;
                        break;
                    case "titanium":
                        profile.Titanium += 1;
                        break;
                    default:
                        profile.Fuel += 2;
                        break;
                }

                profile.Experience += 10;

                while (profile.Experience >= profile.NextLevelExp)
                {
                    profile.Experience -= profile.NextLevelExp;
                    profile.Level += 1;
                    profile.NextLevelExp = (int)(profile.NextLevelExp * 1.2);
                }

                SaveDatabase();
            }
        }

        private static bool IsValidColor(string colorHex)
        {
            if (string.IsNullOrWhiteSpace(colorHex) || colorHex.Length != 7 || colorHex[0] != '#')
                return false;

            return colorHex.Skip(1).All(Uri.IsHexDigit);
        }

        private static string GenerateStableColor(string username)
        {
            var hash = Math.Abs(username.GetHashCode());
            var r = 100 + (hash % 156);
            var g = 100 + ((hash / 97) % 156);
            var b = 100 + ((hash / 193) % 156);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
