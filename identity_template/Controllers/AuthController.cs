using identity_template.AuthDTOs;
using identity_template.Data;
using identity_template.Models;
using identity_template.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace identity_template.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] - not needed here as user doesn't have token yet
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, DataContext context, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        [HttpPost("register-user")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO payload)
        {
            var userExists = await _userManager.FindByEmailAsync(payload.Email);
            if(userExists != null) return BadRequest("user already exists");

            Users newUser = new Users()
            {
                UserName = payload.UserName,
                Email = payload.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            var result = await _userManager.CreateAsync(newUser, payload.Password);
            if(!result.Succeeded) return BadRequest("failed to create user");

            return Created(nameof(Register), "user created successfully");
        }

        public async Task<IActionResult> Login([FromBody] LoginDTO payload)
        {
            if(!ModelState.IsValid) return BadRequest("provide all required details");

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if(user != null && await _userManager.CheckPasswordAsync(user, payload.Password))
            {
                var token = await GenerateJwtToken(user);
                return Ok(token);
            }
            return Unauthorized();
        }

        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDTO payload)
        {
            if (!ModelState.IsValid) return BadRequest("provide all required details");
            try
            {
                var result = await VerifyAndGenerateToken(payload);
                if(result == null) return BadRequest("Invalid tokens");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<AuthResultDTO> GenerateJwtToken(Users user, string? refreshToken = null)
        {
            var authclaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var authSigninKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddMinutes(1), //Set as desired
                claims: authclaims,
                signingCredentials: new SigningCredentials(authSigninKey, SecurityAlgorithms.HmacSha256)
            );
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            var dbRefreshToken = new RefreshToken();
            if(!string.IsNullOrEmpty(refreshToken))
            {
                dbRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
                if (dbRefreshToken != null)
                {
                    dbRefreshToken.IsRevoked = true;
                    _context.RefreshTokens.Update(dbRefreshToken);
                    await _context.SaveChangesAsync();
                }
            }
            else { 
                dbRefreshToken = new RefreshToken
                {
                    Token = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    JwtId = token.Id,
                    IsRevoked = false,
                    AddedDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddMonths(6) //Set as desired
                };
            }
            await _context.RefreshTokens.AddAsync(dbRefreshToken);
            await _context.SaveChangesAsync();
            var response = new AuthResultDTO
            {
                Token = jwtToken,
                RefreshToken = dbRefreshToken.Token,
                ExpiresAt = token.ValidTo
            };
            return response;
        }
        
        private async Task<AuthResultDTO> VerifyAndGenerateToken(TokenRequestDTO payload)
        {
            try
            {
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var tokenInVerification = jwtTokenHandler.ValidateToken(payload.Token, _tokenValidationParameters, out var validatedToken);//check format
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (!result) return null;
                }//check encryption algorithm
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDate = UnixTimeStampToDateTimeInUtc(utcExpiryDate);
                if (expiryDate > DateTime.UtcNow) throw new Exception("Token has not expired yet");//check expiry date
                var dbRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == payload.RefreshToken);
                if (dbRefreshToken == null) throw new Exception("Refresh token does not exist");//check if it exists in database
                else
                {
                    var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                    if (dbRefreshToken.JwtId != jti) throw new Exception("does not exist");
                    if (dbRefreshToken.ExpiryDate < DateTime.UtcNow) throw new Exception("Refresh token has expired");
                    if (dbRefreshToken.IsRevoked) throw new Exception("Refresh token has been revoked");//check if id is valid
                    var dbUserData = await _userManager.FindByIdAsync(dbRefreshToken.UserId);
                    var newToken = await GenerateJwtToken(dbUserData, payload.RefreshToken);
                    return newToken;
                }
            }catch(SecurityTokenExpiredException ex)
            {
                var dbRefreshTokenInCatch = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == payload.RefreshToken);
                var dbUserData = await _userManager.FindByIdAsync(dbRefreshTokenInCatch.UserId);
                var newToken = await GenerateJwtToken(dbUserData);
                return newToken;
            }catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        private DateTime UnixTimeStampToDateTimeInUtc(long unixTimeStamp)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTimeVal;
        }
    }
}
