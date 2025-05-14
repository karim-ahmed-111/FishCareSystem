using Microsoft.AspNetCore.Mvc;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using FishCareSystem.API.Data;
using FishCareSystem.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using FishCareSystem.API.Services.Interface;
using System.Net;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly FishCareDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            FishCareDbContext context,
            ILogger<AuthController> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Registration failed for username {registerDto.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

            await _userManager.AddToRoleAsync(user, "Manager");
            var (accessToken, refreshToken) = await GenerateTokens(user);
            _logger.LogInformation($"User registered successfully: {registerDto.UserName}");
            return Ok(new AuthResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            ApplicationUser user;
            if (loginDto.UserName.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(loginDto.UserName);
            }
            else
            {
                user = await _userManager.FindByNameAsync(loginDto.UserName);
            }
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                _logger.LogWarning($"Login failed for username or email: {loginDto.UserName}");
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username/email or password"
                });
            }

            var (accessToken, refreshToken) = await GenerateTokens(user);
            _logger.LogInformation($"User logged in successfully: {loginDto.UserName}");
            return Ok(new AuthResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshDto)
        {
            var principal = GetPrincipalFromExpiredToken(refreshDto.AccessToken);
            if (principal == null)
            {
                _logger.LogWarning("Invalid access token during token refresh");
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid access token"
                });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found for ID: {userId} during token refresh");
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var storedRefreshToken = _context.RefreshTokens
                .FirstOrDefault(rt => rt.UserId == userId && rt.Token == refreshDto.RefreshToken && rt.Expires > DateTime.UtcNow);
            if (storedRefreshToken == null)
            {
                _logger.LogWarning($"Invalid or expired refresh token for user ID: {userId}");
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                });
            }

            // Generate new tokens
            var (newAccessToken, newRefreshToken) = await GenerateTokens(user);

            // Remove old refresh token
            _context.RefreshTokens.Remove(storedRefreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Token refreshed successfully for user: {user.UserName}");
            return Ok(new AuthResponseDto
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Do not reveal that the user does not exist
                return Ok(new { message = "If the email is registered, a reset link has been sent." });
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = $"https://your-frontend/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(dto.Email)}";
            var subject = "FishCareSystem Password Reset";
            var body = $"<p>Click <a href='{resetUrl}'>here</a> to reset your password.</p>";
            await _emailService.SendEmailAsync(dto.Email, subject, body);
            return Ok(new { message = "If the email is registered, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid request." });
            }
            var decodedToken = WebUtility.UrlDecode(dto.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(new UserProfileDto
            {
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile updated successfully." });
        }

        private async Task<(string AccessToken, string RefreshToken)> GenerateTokens(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var accessToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpireMinutes"])),
                signingCredentials: creds);

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            // Generate refresh token
            var refreshTokenString = Guid.NewGuid().ToString();
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpireDays"]))
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return (accessTokenString, refreshTokenString);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false // Allow expired tokens
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}