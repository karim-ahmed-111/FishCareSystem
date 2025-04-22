using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }
    
    public class LoginDto
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
    public class RefreshTokenDto
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Message { get; set; }
    }



    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
