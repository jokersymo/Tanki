using System.Text.Json.Serialization;

namespace TankiLauncher.Models
{
    public class Profile
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("Level")]
        public int Level { get; set; }

        [JsonPropertyName("Experience")]
        public int Experience { get; set; }

        [JsonPropertyName("NextLevelExp")]
        public int NextLevelExp { get; set; }

        [JsonPropertyName("Silver")]
        public int Silver { get; set; }

        [JsonPropertyName("Gold")]
        public int Gold { get; set; }

        [JsonPropertyName("Iron")]
        public int Iron { get; set; }

        [JsonPropertyName("Copper")]
        public int Copper { get; set; }

        [JsonPropertyName("Titanium")]
        public int Titanium { get; set; }

        [JsonPropertyName("Fuel")]
        public int Fuel { get; set; }

        [JsonPropertyName("Battles")]
        public int Battles { get; set; }

        [JsonPropertyName("Wins")]
        public int Wins { get; set; }

        [JsonPropertyName("KD")]
        public double KD { get; set; }

        [JsonPropertyName("Rank")]
        public string Rank { get; set; } = string.Empty;
    }
}
