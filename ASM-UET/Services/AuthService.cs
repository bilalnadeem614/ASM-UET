using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ASM_UET.Models;

namespace ASM_UET.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest req);
        Task<AuthResponse?> LoginAsync(LoginRequest req);
    }

    public class AuthService : IAuthService
    {
        private readonly ASM _db;
        private readonly IConfiguration _config;

        public AuthService(ASM db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
        {
            // map role string to int
            int role = req.Role.ToLower() == "teacher" ? 1 : 2;

            // simple existing email check
            var existing = _db.Users.FirstOrDefault(u => u.Email == req.Email);
            if (existing != null)
            {
                throw new Exception("Email already registered");
            }

            // NOTE: For demo purposes only. Use a proper password hasher like ASP.NET Core Identity in real apps.
            var user = new User
            {
                FullName = req.FullName,
                Email = req.Email,
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req.Password)),
                Role = role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = GenerateJwt(user);
            return new AuthResponse { Token = token, Role = user.Role };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest req)
        {
            var hashed = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req.Password));
            var user = _db.Users.FirstOrDefault(u => u.Email == req.Email && u.PasswordHash == hashed);
            if (user == null) return null;

            var token = GenerateJwt(user);
            return new AuthResponse { Token = token, Role = user.Role };
        }

        private string GenerateJwt(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = jwt.GetValue<string>("Key")!;
            var issuer = jwt.GetValue<string>("Issuer")!;
            var audience = jwt.GetValue<string>("Audience")!;
            var expireMinutes = jwt.GetValue<int>("ExpireMinutes");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString())
            };

            var token = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
