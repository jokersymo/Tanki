using Microsoft.AspNetCore.SignalR;
using TankiServer.Models;

namespace TankiServer.Hubs
{
    public class GameHub : Hub
    {

        const int CHUNK_SIZE = 500;
        const int SPAWN_RADIUS = 5; // сколько чанков вокруг центра
        Random random = new Random();

        (double x, double y) GetSpawnPosition()
        {
            int chunkX = random.Next(-SPAWN_RADIUS, SPAWN_RADIUS + 1);
            int chunkY = random.Next(-SPAWN_RADIUS, SPAWN_RADIUS + 1);

            double x = chunkX * CHUNK_SIZE + random.Next(CHUNK_SIZE);
            double y = chunkY * CHUNK_SIZE + random.Next(CHUNK_SIZE);

            return (x, y);
        }


        static List<ResourceNode> resources = new List<ResourceNode>();

        void GenerateResources()
        {
            for(int i = 0; i < 200; i++)
            {
                var r = new ResourceNode
                {
                    X = random.Next(-3000,3000),
                    Y = random.Next(-3000,3000),
                    Type = RandomType()
                };

                resources.Add(r);
            }
        }
        static bool generated = false;

        public GameHub()
        {
            if(!generated)
            {
                GenerateResources();
                generated = true;

                Console.WriteLine($"Resources generated: {resources.Count}");
            }
        }
        string RandomType()
        {
            int t = random.Next(6);

            return t switch
            {
                0 => "iron",
                1 => "silver",
                2 => "gold",
                3 => "copper",
                4 => "titanium",
                _ => "fuel"
            };
        }

        public async Task PickupResource(string id)
        {
            var res = resources.FirstOrDefault(r => r.Id == id);

            if(res == null)
                return;

            resources.Remove(res);

            await Clients.All.SendAsync("ResourceRemoved", id);

            Console.WriteLine($"Resource {id} collected");
        }

        public static Dictionary<string, PlayerState> players = new();

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Player connected {Context.ConnectionId}");

            var spawn = GetSpawnPosition();

            players[Context.ConnectionId] = new PlayerState
            {
                Id = Context.ConnectionId,
                X = 0,
                Y = 0
            };

            await Clients.All.SendAsync("PlayersUpdate", players);
            await Clients.Caller.SendAsync("Resources", resources);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            players.Remove(Context.ConnectionId);

            await Clients.All.SendAsync("PlayersUpdate", players);

            Console.WriteLine($"Player disconnected {Context.ConnectionId}");

            await base.OnDisconnectedAsync(ex);
        }

        public async Task Move(double x, double y)
        {
            if (!players.ContainsKey(Context.ConnectionId))
                return;

            players[Context.ConnectionId].X = x;
            players[Context.ConnectionId].Y = y;

            await Clients.All.SendAsync("PlayersUpdate", players);
        }
    }
}