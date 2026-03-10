namespace TankiServer.Models
{
    public class PlayerState
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = "P";
        public string Letter { get; set; } = "P";
        public string ColorHex { get; set; } = "#3B82F6";
        public double PulseScale { get; set; } = 1.0;
        public double X { get; set; }
        public double Y { get; set; }
    }
}
