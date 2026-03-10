using TankiServer.Services;
using TankiServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// контроллеры API
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 🔴 Регистрируем сервис пользователей
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

// маршруты контроллеров
app.MapControllers();
app.MapHub<GameHub>("/game");

// запуск сервера
app.Run("http://localhost:5000");