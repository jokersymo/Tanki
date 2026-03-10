using Microsoft.AspNetCore.Mvc;
using TankiServer.Services;

namespace TankiServer.Controllers
{
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly UserService _users;

        public ProfileController(UserService users)
        {
            _users = users;
        }

        [HttpGet("/profile/{username}")]
        public IActionResult GetProfile(string username)
        {
            Console.WriteLine($"[SERVER] Запрос профиля: {username}");

            var user = _users.GetProfile(username);

            if (user == null)
            {
                Console.WriteLine($"[SERVER] Игрок НЕ найден: {username}");
                return NotFound();
            }

            Console.WriteLine($"[SERVER] Профиль найден");

            Console.WriteLine($"Username: {user.Username}");
            Console.WriteLine($"Silver: {user.Silver}");
            Console.WriteLine($"Gold: {user.Gold}");
            Console.WriteLine($"Iron: {user.Iron}");
            Console.WriteLine($"Copper: {user.Copper}");
            Console.WriteLine($"Titanium: {user.Titanium}");
            Console.WriteLine($"Fuel: {user.Fuel}");

            return Ok(user);
        }
    }
}