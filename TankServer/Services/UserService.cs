using System.Text.Json;
using TankiServer.Models;

namespace TankiServer.Services
{
    public class UserService
    {
        private static Dictionary<string, string> users = new();
        private static Dictionary<string, PlayerProfile> profiles = new();

        private static string dbPath = "Data/players.json";

        // =============================
        // ЗАГРУЗКА БАЗЫ
        // =============================

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

            profiles = JsonSerializer.Deserialize
            <Dictionary<string, PlayerProfile>>(json)
            ?? new Dictionary<string, PlayerProfile>();

            foreach (var p in profiles)
            {
                users[p.Key] = "password";
            }

            Console.WriteLine($"[DB] Loaded players: {profiles.Count}");

            foreach (var p in profiles)
            {
                Console.WriteLine($"[DB] Player: {p.Key}");
            }
        }

        // =============================
        // СОХРАНЕНИЕ
        // =============================

        public static void SaveDatabase()
        {
            Console.WriteLine("[DB] Saving database...");

            var json = JsonSerializer.Serialize(
                profiles,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(dbPath, json);

            Console.WriteLine("[DB] Saved players");
        }

        // =============================
        // РЕГИСТРАЦИЯ
        // =============================

        public bool Register(string username, string password)
        {
            Console.WriteLine($"[REGISTER] Attempt: {username}");

            if (profiles.ContainsKey(username))
            {
                Console.WriteLine("[REGISTER] User already exists");
                return false;
            }

            users[username] = password;

            profiles[username] = new PlayerProfile
            {
                Username = username
            };

            SaveDatabase();

            Console.WriteLine($"[REGISTER] Success: {username}");

            return true;
        }

        // =============================
        // ЛОГИН
        // =============================

        public bool Login(string username, string password)
        {
            Console.WriteLine($"[LOGIN] Attempt: {username}");

            if (!users.ContainsKey(username))
            {
                Console.WriteLine("[LOGIN] User not found");
                return false;
            }

            Console.WriteLine($"[LOGIN] Success: {username}");

            return true;
        }

        // =============================
        // ПОЛУЧЕНИЕ ПРОФИЛЯ
        // =============================

        public PlayerProfile? GetProfile(string username)
        {
            Console.WriteLine($"[PROFILE REQUEST] {username}");

            if (profiles.ContainsKey(username))
            {
                Console.WriteLine("[PROFILE] Found player");

                var p = profiles[username];

                Console.WriteLine($"Silver: {p.Silver}");
                Console.WriteLine($"Gold: {p.Gold}");
                Console.WriteLine($"Iron: {p.Iron}");
                Console.WriteLine($"Copper: {p.Copper}");
                Console.WriteLine($"Titanium: {p.Titanium}");

                return p;
            }

            Console.WriteLine("[PROFILE] Player not found");

            return null;
        }
    }
}