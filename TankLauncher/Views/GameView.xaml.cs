using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;

namespace TankiLauncher.Views
{
    public partial class GameView : UserControl
    {
        HubConnection connection;

        Dictionary<string, Ellipse> otherPlayers = new();
        Dictionary<string, TextBlock> otherPlayerTexts = new();
        Dictionary<string, PlayerState> serverPlayers = new();

        TextBlock fpsText;

        bool showFPS = false;

        int frameCount = 0;
        DateTime lastFPSUpdate = DateTime.Now;
        int chunkSize = 600;
        HashSet<string> generatedChunks = new HashSet<string>();

        Ellipse player;
        TextBlock playerText;

        double playerX = 0;
        double playerY = 0;

        double speed = 6;

        HashSet<Key> keys = new HashSet<Key>();

        Random random = new Random();

        List<Resource> resources = new List<Resource>();
        List<TrailPoint> trail = new List<TrailPoint>();

        void UpdatePlayers(Dictionary<string, PlayerState> players)
        {
            foreach(var p in players)
            {
                if(p.Key == connection.ConnectionId)
                    continue;

                if(!otherPlayers.ContainsKey(p.Key))
                {
                    Ellipse circle = new Ellipse
                    {
                        Width = 40,
                        Height = 40,
                        Fill = Brushes.Blue
                    };

                    TextBlock txt = new TextBlock
                    {
                        Text = "[P]",
                        FontWeight = FontWeights.Bold
                    };

                    GameCanvas.Children.Add(circle);
                    GameCanvas.Children.Add(txt);

                    otherPlayers[p.Key] = circle;
                    otherPlayerTexts[p.Key] = txt;
                }

                double centerX = ActualWidth / 2;
                double centerY = ActualHeight / 2;

                double x = p.Value.X - playerX + centerX;
                double y = p.Value.Y - playerY + centerY;

                Canvas.SetLeft(otherPlayers[p.Key], x - 20);
                Canvas.SetTop(otherPlayers[p.Key], y - 20);

                Canvas.SetLeft(otherPlayerTexts[p.Key], x - 10);
                Canvas.SetTop(otherPlayerTexts[p.Key], y - 10);
            }
        }

        public GameView()
        {
            InitializeComponent();
            Loaded += GameLoaded;
        }

        void UpdateChunks()
        {
            int playerChunkX = (int)Math.Floor(playerX / chunkSize);
            int playerChunkY = (int)Math.Floor(playerY / chunkSize);

            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    GenerateChunk(playerChunkX + x, playerChunkY + y);
                }
            }
        }
        void GenerateChunk(int cx, int cy)
        {
            string key = cx + "_" + cy;

            if (generatedChunks.Contains(key))
                return;

            generatedChunks.Add(key);

            for (int i = 0; i < 12; i++)
            {
                Rectangle rect = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Fill = RandomResourceColor()
                };

                double x = cx * chunkSize + random.Next(chunkSize);
                double y = cy * chunkSize + random.Next(chunkSize);

                Resource res = new Resource
                {
                    worldX = x,
                    worldY = y,
                    rect = rect
                };

                resources.Add(res);
                GameCanvas.Children.Add(rect);
            }
        }
        Color Darken(Color c)
        {
            return Color.FromRgb(
                (byte)(c.R * 0.6),
                (byte)(c.G * 0.6),
                (byte)(c.B * 0.6));
        }
        void StartPulse()
        {
            DispatcherTimer pulse = new DispatcherTimer();
            pulse.Interval = TimeSpan.FromMilliseconds(4);

            double t = 0;

            pulse.Tick += (s, e) =>
            {
                t += 0.05;

                double scale = 1 + Math.Sin(t) * 0.07;

                player.Width = 40 * scale;
                player.Height = 40 * scale;

                playerText.FontSize = 14 * scale;
            };

            pulse.Start();
        }
        void CreateFPSCounter()
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
        void GameLoaded(object sender, RoutedEventArgs e)
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

        void UpdateOtherPlayers()
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            foreach(var p in serverPlayers)
            {
                if(p.Key == connection.ConnectionId)
                    continue;

                if(!otherPlayers.ContainsKey(p.Key))
                    continue;

                double worldX = p.Value.X;
                double worldY = p.Value.Y;

                double screenX = worldX - playerX + centerX;
                double screenY = worldY - playerY + centerY;

                Canvas.SetLeft(otherPlayers[p.Key], screenX - 20);
                Canvas.SetTop(otherPlayers[p.Key], screenY - 20);

                Canvas.SetLeft(otherPlayerTexts[p.Key], screenX - 10);
                Canvas.SetTop(otherPlayerTexts[p.Key], screenY - 10);
            }
        }

        void CreatePlayer()
        {
            Color baseColor = RandomColor();

            player = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(baseColor, 0.4),
                        new GradientStop(Darken(baseColor), 1)
                    }
                }
            };

            GameCanvas.Children.Add(player);

            string name = App.CurrentUser ?? "P";
            string letter = name.Substring(0, 1).ToUpper();

            playerText = new TextBlock
            {
                Text = "[" + letter + "]",
                FontWeight = FontWeights.Bold
            };

            GameCanvas.Children.Add(playerText);
            StartPulse();
        }

        Color RandomColor()
        {
            return Color.FromRgb(
                (byte)random.Next(100,255),
                (byte)random.Next(100,255),
                (byte)random.Next(100,255));
        }
        void UpdateFPS()
        {
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
        void SpawnResources()
        {
            for(int i = 0; i < 80; i++)
            {
                Rectangle rect = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Fill = RandomResourceColor()
                };

                Resource res = new Resource
                {
                    worldX = random.Next(-2000,2000),
                    worldY = random.Next(-2000,2000),
                    rect = rect
                };

                resources.Add(res);
                GameCanvas.Children.Add(rect);
            }
        }

        Brush RandomResourceColor()
        {
            int type = random.Next(6);

            return type switch
            {
                0 => Brushes.Gray,
                1 => Brushes.White,
                2 => Brushes.Orange,
                3 => Brushes.IndianRed,
                4 => Brushes.DarkGray,
                _ => Brushes.Red
            };
        }

        void KeyPressed(object sender, KeyEventArgs e)
        {
            keys.Add(e.Key);
            if (e.Key == Key.P)
            {
                showFPS = !showFPS;

                fpsText.Visibility = showFPS
                    ? Visibility.Visible
                    : Visibility.Hidden;
            }
        }

        void KeyReleased(object sender, KeyEventArgs e)
        {
            keys.Remove(e.Key);
        }

        void GameLoop(object? sender, EventArgs e)
        {
            bool moving = false;

            if (keys.Contains(Key.W)) { playerY -= speed; moving = true; }
            if (keys.Contains(Key.S)) { playerY += speed; moving = true; }
            if (keys.Contains(Key.A)) { playerX -= speed; moving = true; }
            if (keys.Contains(Key.D)) { playerX += speed; moving = true; }

            if (moving)
            {
                if (random.Next(3) == 0)
                {
                    CreateTrail();
                }
            }
            UpdateTrail();
            UpdateChunks();
            UpdatePlayerPosition();
            UpdateResources();
            CheckResources();
            UpdateFPS();
            UpdateOtherPlayers();
            if(connection != null)
            {
                connection.SendAsync("Move", playerX, playerY);
            }
        }

        void UpdatePlayerPosition()
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            double size = player.Width;

            Canvas.SetLeft(player, centerX - size / 2);
            Canvas.SetTop(player, centerY - size / 2);

            playerText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double textW = playerText.DesiredSize.Width;
            double textH = playerText.DesiredSize.Height;

            Canvas.SetLeft(playerText, centerX - textW / 2);
            Canvas.SetTop(playerText, centerY - textH / 2);
        }

        void UpdateResources()
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            foreach(var res in resources)
            {
                double screenX = res.worldX - playerX + centerX;
                double screenY = res.worldY - playerY + centerY;

                Canvas.SetLeft(res.rect, screenX);
                Canvas.SetTop(res.rect, screenY);
            }
        }

        void CheckResources()
        {
            foreach(var res in resources.ToArray())
            {
                double dx = res.worldX - playerX;
                double dy = res.worldY - playerY;

                if(Math.Abs(dx) < 25 && Math.Abs(dy) < 25)
                {
                    GameCanvas.Children.Remove(res.rect);
                    resources.Remove(res);

                    Console.WriteLine("Ресурс собран +10 XP");
                }
            }
        }

        void CreateTrail()
        {
            Ellipse dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Gray,
                Opacity = 0.4
            };

            TrailPoint t = new TrailPoint
            {
                worldX = playerX,
                worldY = playerY,
                circle = dot
            };

            GameCanvas.Children.Add(dot);
            trail.Add(t);
        }

        void UpdateTrail()
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            foreach (var t in trail.ToArray())
            {
                t.life -= 0.016;

                double screenX = t.worldX - playerX + centerX;
                double screenY = t.worldY - playerY + centerY;

                Canvas.SetLeft(t.circle, screenX);
                Canvas.SetTop(t.circle, screenY);

                t.circle.Opacity = t.life / 3.0;

                if (t.life <= 0)
                {
                    GameCanvas.Children.Remove(t.circle);
                    trail.Remove(t);
                }
            }
        }
        void CreatePlayersIfNeeded(Dictionary<string, PlayerState> players)
        {
            foreach(var p in players)
            {
                if(p.Key == connection.ConnectionId)
                    continue;

                if(!otherPlayers.ContainsKey(p.Key))
                {
                    Ellipse circle = new Ellipse
                    {
                        Width = 40,
                        Height = 40,
                        Fill = Brushes.Blue
                    };

                    TextBlock txt = new TextBlock
                    {
                        Text = "[P]",
                        FontWeight = FontWeights.Bold
                    };

                    GameCanvas.Children.Add(circle);
                    GameCanvas.Children.Add(txt);

                    otherPlayers[p.Key] = circle;
                    otherPlayerTexts[p.Key] = txt;
                }
            }
        }
        async void ConnectToServer()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/game")
                .WithAutomaticReconnect()
                .Build();

            connection.On<Dictionary<string, PlayerState>>("PlayersUpdate", (players) =>
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var p in players)
                    {
                        if (p.Key == connection.ConnectionId)
                            continue; // ← игнорируем себя

                        serverPlayers[p.Key] = p.Value;
                    }

                    CreatePlayersIfNeeded(serverPlayers);
                });
            });

            await connection.StartAsync();

            Console.WriteLine("Connected to server");
        }
    class Resource
    {
        public double worldX;
        public double worldY;
        public Rectangle rect;
    }
    class TrailPoint
    {
        public double worldX;
        public double worldY;
        public double life = 3.0; // секунды
        public Ellipse circle;
    }
    public class PlayerState
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
}