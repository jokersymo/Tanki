namespace TankiServer.Models
{
    public class ResourceNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public double X { get; set; }

        public double Y { get; set; }

        public string Type { get; set; }
    }
}