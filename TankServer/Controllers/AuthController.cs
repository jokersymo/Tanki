using Microsoft.AspNetCore.Mvc;
using TankiServer.Services;
using TankiServer.Models;

namespace TankiServer.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private UserService users = new UserService();

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthRequest data)
        {
            Console.WriteLine($"[REGISTER] Attempt: {data.Username}");

            bool result = users.Register(data.Username, data.Password);

            if (!result)
            {
                Console.WriteLine($"[REGISTER] User already exists: {data.Username}");
                return BadRequest("User exists");
            }

            Console.WriteLine($"[REGISTER] Success: {data.Username}");

            return Ok("Registered");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest data)
        {
            Console.WriteLine($"[LOGIN] Attempt: {data.Username}");

            bool result = users.Login(data.Username, data.Password);

            if (!result)
            {
                Console.WriteLine($"[LOGIN] Failed: {data.Username}");
                return Unauthorized();
            }

            Console.WriteLine($"[LOGIN] Success: {data.Username}");

            return Ok("Login success");
        }
    }
}