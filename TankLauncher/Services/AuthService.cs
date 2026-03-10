using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TankiLauncher.Services
{
    public class AuthService
    {
        private HttpClient client = new HttpClient();

        public async Task<bool> Login(string username, string password)
        {
            var data = new
            {
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(data);

            var response = await client.PostAsync(
                "http://localhost:5000/auth/login",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Register(string username, string password)
        {
            var data = new
            {
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(data);

            var response = await client.PostAsync(
                "http://localhost:5000/auth/register",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            return response.IsSuccessStatusCode;
        }
    }
}