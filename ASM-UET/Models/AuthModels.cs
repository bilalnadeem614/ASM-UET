using System.ComponentModel.DataAnnotations;

namespace ASM_UET.Models
{
    public class RegisterRequest
    {
        [Required]
        public string Role { get; set; } = null!; // "Teacher" or "Student"

        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }

    public class LoginRequest
    {
        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public int Role { get; set; }
    }
}