namespace TankiServer.Models
{
    public class PlayerProfile
    {
        public string Username { get; set; } = string.Empty;
        public string PlayerColorHex { get; set; } = "#3B82F6";

        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int NextLevelExp { get; set; } = 1000;

        public int Silver { get; set; } = 500;
        public int Gold { get; set; } = 10;

        public int Iron { get; set; } = 50;
        public int Copper { get; set; } = 40;
        public int Titanium { get; set; } = 5;

        public int Fuel { get; set; } = 100;

        public int Battles { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public float KD { get; set; } = 1.0f;

        public string Rank { get; set; } = "Новобранец";
    }
}
