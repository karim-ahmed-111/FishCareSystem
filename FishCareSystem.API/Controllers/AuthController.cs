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

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, FishCareDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
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
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

            await _userManager.AddToRoleAsync(user, "Manager");
            var (accessToken, refreshToken) = await GenerateTokens(user);
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
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            var (accessToken, refreshToken) = await GenerateTokens(user);
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

            return Ok(new AuthResponseDto
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
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
    //private async Task<string> GenerateJwtToken(ApplicationUser user)
    //{
    //    var claims = new[]
    //    {
    //        new Claim(ClaimTypes.NameIdentifier, user.Id),
    //        new Claim(ClaimTypes.Name, user.UserName),
    //        new Claim(ClaimTypes.Role, "Manager")
    //    };

    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    //    var token = new JwtSecurityToken(
    //        issuer: _configuration["Jwt:Issuer"],
    //        audience: _configuration["Jwt:Audience"],
    //        claims: claims,
    //        expires: DateTime.Now.AddDays(7),
    //        signingCredentials: creds);

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
}

