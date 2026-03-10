using Microsoft.AspNetCore.SignalR;
using TankiServer.Models;
using TankiServer.Services;

namespace TankiServer.Hubs
{
    public class GameHub : Hub
    {
        private const int SpawnRadius = 5;
        private const int ChunkSize = 500;
        private const double AttractionRadius = 10;
        private const double FollowSpeed = 1.6;

        private static readonly Random random = new();
        private static readonly object sync = new();
        private static readonly List<ResourceNode> resources = new();
        private static readonly Dictionary<string, PlayerState> players = new();
        private static readonly Dictionary<string, string> connectionsByUsername = new(StringComparer.OrdinalIgnoreCase);
        private static bool generated;

        private readonly UserService userService;

        public GameHub(UserService userService)
        {
            this.userService = userService;

            lock (sync)
            {
                if (!generated)
                {
                    GenerateResources();
                    generated = true;
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("ResourcesUpdate", SnapshotResources());
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            lock (sync)
            {
                if (players.TryGetValue(Context.ConnectionId, out var disconnectedPlayer))
                {
                    connectionsByUsername.Remove(disconnectedPlayer.Username);
                }

                players.Remove(Context.ConnectionId);

                foreach (var resource in resources.Where(r => r.TargetPlayerId == Context.ConnectionId))
                {
                    resource.TargetPlayerId = null;
                }
            }

            await BroadcastPlayers();
            await base.OnDisconnectedAsync(ex);
        }

        public async Task JoinGame(string username, string requestedColorHex)
        {
            username = string.IsNullOrWhiteSpace(username) ? "P" : username.Trim();
            var letter = username.Substring(0, 1).ToUpperInvariant();
            var colorHex = userService.EnsurePlayerColor(username, requestedColorHex);
            var spawn = GetSpawnPosition();

            string? staleConnectionId = null;

            lock (sync)
            {
                if (connectionsByUsername.TryGetValue(username, out var existingConnectionId) && existingConnectionId != Context.ConnectionId)
                {
                    staleConnectionId = existingConnectionId;
                    players.Remove(existingConnectionId);
                }

                connectionsByUsername[username] = Context.ConnectionId;
                players[Context.ConnectionId] = new PlayerState
                {
                    Id = Context.ConnectionId,
                    Username = username,
                    Letter = letter,
                    ColorHex = colorHex,
                    X = spawn.x,
                    Y = spawn.y,
                    PulseScale = 1
                };
            }

            if (staleConnectionId != null)
            {
                await Clients.Client(staleConnectionId).SendAsync("ForceDisconnect", "Выполнен вход с нового соединения");
            }

            await BroadcastPlayers();
            await Clients.All.SendAsync("ResourcesUpdate", SnapshotResources());
        }

        public async Task Move(double x, double y, double pulseScale)
        {
            lock (sync)
            {
                if (!players.TryGetValue(Context.ConnectionId, out var player))
                    return;

                player.X = x;
                player.Y = y;
                player.PulseScale = pulseScale;

                UpdateResourceAttraction();
                MoveResources();
            }

            await BroadcastPlayers();
            await Clients.All.SendAsync("ResourcesUpdate", SnapshotResources());
        }

        private void UpdateResourceAttraction()
        {
            foreach (var resource in resources)
            {
                if (resource.TargetPlayerId != null)
                    continue;

                foreach (var player in players.Values)
                {
                    var dx = player.X - resource.X;
                    var dy = player.Y - resource.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= AttractionRadius)
                    {
                        resource.TargetPlayerId = player.Id;
                        break;
                    }
                }
            }
        }

        private void MoveResources()
        {
            var collected = new List<ResourceNode>();

            foreach (var resource in resources)
            {
                if (resource.TargetPlayerId == null)
                    continue;

                if (!players.TryGetValue(resource.TargetPlayerId, out var player))
                {
                    resource.TargetPlayerId = null;
                    continue;
                }

                var dx = player.X - resource.X;
                var dy = player.Y - resource.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 1.8)
                {
                    collected.Add(resource);
                    continue;
                }

                var nx = dx / distance;
                var ny = dy / distance;

                resource.X += nx * FollowSpeed;
                resource.Y += ny * FollowSpeed;
            }

            foreach (var resource in collected)
            {
                resources.Remove(resource);

                if (players.TryGetValue(resource.TargetPlayerId!, out var player))
                {
                    userService.ApplyResourcePickup(player.Username, resource.Type);
                }
            }
        }

        private async Task BroadcastPlayers()
        {
            Dictionary<string, PlayerState> snapshot;
            lock (sync)
            {
                snapshot = players.ToDictionary(p => p.Key, p => p.Value);
            }

            await Clients.All.SendAsync("PlayersUpdate", snapshot);
        }

        private static List<ResourceNode> SnapshotResources()
        {
            lock (sync)
            {
                return resources
                    .Select(r => new ResourceNode
                    {
                        Id = r.Id,
                        X = r.X,
                        Y = r.Y,
                        Type = r.Type,
                        TargetPlayerId = r.TargetPlayerId
                    })
                    .ToList();
            }
        }

        private static (double x, double y) GetSpawnPosition()
        {
            var chunkX = random.Next(-SpawnRadius, SpawnRadius + 1);
            var chunkY = random.Next(-SpawnRadius, SpawnRadius + 1);

            var x = chunkX * ChunkSize + random.Next(ChunkSize);
            var y = chunkY * ChunkSize + random.Next(ChunkSize);

            return (x, y);
        }

        private static void GenerateResources()
        {
            for (var i = 0; i < 200; i++)
            {
                resources.Add(new ResourceNode
                {
                    X = random.Next(-3000, 3000),
                    Y = random.Next(-3000, 3000),
                    Type = RandomType()
                });
            }
        }

        private static string RandomType()
        {
            var t = random.Next(6);
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
    }
}
