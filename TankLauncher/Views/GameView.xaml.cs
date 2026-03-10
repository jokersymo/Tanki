using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TankiLauncher.Views
{
    public partial class GameView : UserControl
    {
        private HubConnection? connection;

        private readonly Dictionary<string, VisualPlayer> otherPlayers = new();
        private readonly Dictionary<string, PlayerState> serverPlayers = new();
        private readonly Dictionary<string, ResourceVisual> resources = new();
        private readonly HashSet<Key> keys = new();
        private readonly Random random = new();

        private readonly List<TrailPoint> trail = new();

        private TextBlock? fpsText;
        private bool showFPS;
        private int frameCount;
        private DateTime lastFPSUpdate = DateTime.Now;

        private Ellipse? player;
        private TextBlock? playerText;
        private string playerColorHex = "#4DA3FF";

        private double playerX;
        private double playerY;
        private readonly double speed = 6;

        public GameView()
        {
            InitializeComponent();
            Loaded += GameLoaded;
            Unloaded += GameUnloaded;
        }

        private void GameLoaded(object sender, RoutedEventArgs e)
        {
            Focusable = true;
            Focus();
            Keyboard.Focus(this);

            CreatePlayer();
            PreviewKeyDown += KeyPressed;
            PreviewKeyUp += KeyReleased;
            CompositionTarget.Rendering += GameLoop;
            CreateFPSCounter();
            ConnectToServer();
        }

        private async void GameUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= GameLoop;
            PreviewKeyDown -= KeyPressed;
            PreviewKeyUp -= KeyReleased;

            if (connection != null)
            {
                try
                {
                    await connection.StopAsync();
                    await connection.DisposeAsync();
                }
                catch
                {
                }

                connection = null;
            }
        }

        private void CreateFPSCounter()
        {
            fpsText = new TextBlock
            {
                Text = "FPS: 0",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                Background = Brushes.White,
                Opacity = 0.7,
                Visibility = Visibility.Hidden
            };

            Canvas.SetLeft(fpsText, 10);
            Canvas.SetTop(fpsText, 10);
            GameCanvas.Children.Add(fpsText);
        }

        private void CreatePlayer()
        {
            var baseColor = RandomColor();
            playerColorHex = $"#{baseColor.R:X2}{baseColor.G:X2}{baseColor.B:X2}";

            player = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = CreateGradient(baseColor)
            };
            GameCanvas.Children.Add(player);

            var name = App.CurrentUser ?? "P";
            var letter = name.Substring(0, 1).ToUpperInvariant();

            playerText = new TextBlock
            {
                Text = $"[{letter}]",
                FontWeight = FontWeights.Bold
            };
            GameCanvas.Children.Add(playerText);
        }

        private async void ConnectToServer()
        {
            if (connection != null)
                return;

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/game")
                .WithAutomaticReconnect()
                .Build();

            connection.On<Dictionary<string, PlayerState>>("PlayersUpdate", players =>
            {
                Dispatcher.Invoke(() =>
                {
                    serverPlayers.Clear();
                    foreach (var p in players)
                    {
                        serverPlayers[p.Key] = p.Value;
                    }

                    if (connection != null && serverPlayers.TryGetValue(connection.ConnectionId ?? string.Empty, out var me))
                    {
                        playerX = me.X;
                        playerY = me.Y;

                        if (!string.IsNullOrWhiteSpace(me.ColorHex) && player != null)
                        {
                            var ownColor = ParseColor(me.ColorHex);
                            playerColorHex = me.ColorHex;
                            player.Fill = CreateGradient(ownColor);
                        }

                        if (playerText != null)
                        {
                            playerText.Text = $"[{me.Letter}]";
                        }
                    }

                    SyncOtherPlayers();
                });
            });

            connection.On<List<ResourceState>>("ResourcesUpdate", serverResources =>
            {
                Dispatcher.Invoke(() => SyncResources(serverResources));
            });

            connection.On<string>("ForceDisconnect", async _ =>
            {
                if (connection != null)
                {
                    await connection.StopAsync();
                }
            });

            connection.Reconnected += async _ =>
            {
                if (connection != null)
                {
                    await connection.SendAsync("JoinGame", App.CurrentUser ?? "P", playerColorHex);
                }
            };

            await connection.StartAsync();
            await connection.SendAsync("JoinGame", App.CurrentUser ?? "P", playerColorHex);
        }

        private void SyncOtherPlayers()
        {
            if (connection == null)
                return;

            var stale = otherPlayers.Keys.Where(id => !serverPlayers.ContainsKey(id) || id == connection.ConnectionId).ToList();
            foreach (var id in stale)
            {
                GameCanvas.Children.Remove(otherPlayers[id].Circle);
                GameCanvas.Children.Remove(otherPlayers[id].Text);
                otherPlayers.Remove(id);
            }

            foreach (var pair in serverPlayers)
            {
                if (pair.Key == connection.ConnectionId)
                    continue;

                if (!otherPlayers.TryGetValue(pair.Key, out var visual))
                {
                    visual = new VisualPlayer();
                    GameCanvas.Children.Add(visual.Circle);
                    GameCanvas.Children.Add(visual.Text);
                    otherPlayers[pair.Key] = visual;
                }

                var state = pair.Value;
                visual.Circle.Fill = CreateGradient(ParseColor(state.ColorHex));
                visual.Text.Text = $"[{state.Letter}]";
            }
        }

        private void SyncResources(List<ResourceState> serverResources)
        {
            var ids = serverResources.Select(r => r.Id).ToHashSet();
            var stale = resources.Keys.Where(id => !ids.Contains(id)).ToList();

            foreach (var id in stale)
            {
                GameCanvas.Children.Remove(resources[id].Rect);
                resources.Remove(id);
            }

            foreach (var res in serverResources)
            {
                if (!resources.TryGetValue(res.Id, out var visual))
                {
                    visual = new ResourceVisual
                    {
                        Rect = new Rectangle
                        {
                            Width = 10,
                            Height = 10
                        }
                    };

                    resources[res.Id] = visual;
                    GameCanvas.Children.Add(visual.Rect);
                }

                visual.State = res;
                visual.Rect.Fill = BrushForType(res.Type);
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (player == null || playerText == null)
                return;

            var moving = false;
            if (keys.Contains(Key.W)) { playerY -= speed; moving = true; }
            if (keys.Contains(Key.S)) { playerY += speed; moving = true; }
            if (keys.Contains(Key.A)) { playerX -= speed; moving = true; }
            if (keys.Contains(Key.D)) { playerX += speed; moving = true; }

            if (moving && random.Next(3) == 0)
                CreateTrail();

            var pulseScale = 1 + Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 7) * 0.07;
            player.Width = 40 * pulseScale;
            player.Height = 40 * pulseScale;
            playerText.FontSize = 14 * pulseScale;

            UpdateTrail();
            UpdatePlayerPosition();
            UpdateResources();
            UpdateOtherPlayers();
            UpdateFPS();

            if (connection != null)
            {
                _ = connection.SendAsync("Move", playerX, playerY, pulseScale);
            }
        }

        private void UpdateResources()
        {
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            foreach (var res in resources.Values)
            {
                var screenX = res.State.X - playerX + centerX;
                var screenY = res.State.Y - playerY + centerY;
                Canvas.SetLeft(res.Rect, screenX);
                Canvas.SetTop(res.Rect, screenY);
            }
        }

        private void UpdateOtherPlayers()
        {
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            foreach (var pair in serverPlayers)
            {
                if (connection == null || pair.Key == connection.ConnectionId)
                    continue;
                if (!otherPlayers.TryGetValue(pair.Key, out var visual))
                    continue;

                var s = pair.Value;
                var size = 40 * s.PulseScale;
                visual.Circle.Width = size;
                visual.Circle.Height = size;
                visual.Text.FontSize = 14 * s.PulseScale;

                var screenX = s.X - playerX + centerX;
                var screenY = s.Y - playerY + centerY;

                Canvas.SetLeft(visual.Circle, screenX - size / 2);
                Canvas.SetTop(visual.Circle, screenY - size / 2);
                Canvas.SetLeft(visual.Text, screenX - 10);
                Canvas.SetTop(visual.Text, screenY - 10);
            }
        }

        private void UpdatePlayerPosition()
        {
            if (player == null || playerText == null)
                return;

            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            Canvas.SetLeft(player, centerX - player.Width / 2);
            Canvas.SetTop(player, centerY - player.Height / 2);

            playerText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(playerText, centerX - playerText.DesiredSize.Width / 2);
            Canvas.SetTop(playerText, centerY - playerText.DesiredSize.Height / 2);
        }

        private void CreateTrail()
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Gray,
                Opacity = 0.4
            };

            trail.Add(new TrailPoint { WorldX = playerX, WorldY = playerY, Circle = dot });
            GameCanvas.Children.Add(dot);
        }

        private void UpdateTrail()
        {
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            foreach (var t in trail.ToArray())
            {
                t.Life -= 0.016;
                var screenX = t.WorldX - playerX + centerX;
                var screenY = t.WorldY - playerY + centerY;
                Canvas.SetLeft(t.Circle, screenX);
                Canvas.SetTop(t.Circle, screenY);
                t.Circle.Opacity = t.Life / 3.0;

                if (t.Life <= 0)
                {
                    GameCanvas.Children.Remove(t.Circle);
                    trail.Remove(t);
                }
            }
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            keys.Add(e.Key);
            if (e.Key == Key.P && fpsText != null)
            {
                showFPS = !showFPS;
                fpsText.Visibility = showFPS ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void KeyReleased(object sender, KeyEventArgs e) => keys.Remove(e.Key);

        private void UpdateFPS()
        {
            if (fpsText == null)
                return;

            frameCount++;
            var now = DateTime.Now;
            var delta = now - lastFPSUpdate;
            if (delta.TotalSeconds >= 1)
            {
                fpsText.Text = "FPS: " + frameCount;
                frameCount = 0;
                lastFPSUpdate = now;
            }
        }

        private static RadialGradientBrush CreateGradient(Color baseColor)
        {
            return new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.White, 0),
                    new GradientStop(baseColor, 0.4),
                    new GradientStop(Darken(baseColor), 1)
                }
            };
        }

        private static Color Darken(Color c) => Color.FromRgb((byte)(c.R * 0.6), (byte)(c.G * 0.6), (byte)(c.B * 0.6));

        private Color RandomColor() => Color.FromRgb((byte)random.Next(100, 255), (byte)random.Next(100, 255), (byte)random.Next(100, 255));

        private static Color ParseColor(string hex)
        {
            try
            {
                var parsed = ColorConverter.ConvertFromString(hex);
                return parsed is Color c ? c : Colors.Blue;
            }
            catch
            {
                return Colors.Blue;
            }
        }

        private static Brush BrushForType(string type) => type switch
        {
            "iron" => Brushes.Gray,
            "silver" => Brushes.White,
            "gold" => Brushes.Orange,
            "copper" => Brushes.IndianRed,
            "titanium" => Brushes.DarkGray,
            _ => Brushes.Red
        };

        private sealed class VisualPlayer
        {
            public Ellipse Circle { get; } = new() { Width = 40, Height = 40, Fill = Brushes.Blue };
            public TextBlock Text { get; } = new() { Text = "[P]", FontWeight = FontWeights.Bold };
        }

        private sealed class ResourceVisual
        {
            public required Rectangle Rect { get; init; }
            public ResourceState State { get; set; } = new();
        }

        private sealed class TrailPoint
        {
            public double WorldX;
            public double WorldY;
            public double Life = 3.0;
            public required Ellipse Circle;
        }

        public sealed class PlayerState
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("username")]
            public string Username { get; set; } = "P";

            [JsonPropertyName("letter")]
            public string Letter { get; set; } = "P";

            [JsonPropertyName("colorHex")]
            public string ColorHex { get; set; } = "#3B82F6";

            [JsonPropertyName("pulseScale")]
            public double PulseScale { get; set; } = 1;

            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("y")]
            public double Y { get; set; }
        }

        public sealed class ResourceState
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("y")]
            public double Y { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;
        }
    }
}
