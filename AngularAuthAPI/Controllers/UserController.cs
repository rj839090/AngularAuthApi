using AngularAuthAPI.Context;
using AngularAuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AngularAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDBContext _authContext;
        public UserController(AppDBContext appDBContext)
        {
            _authContext = appDBContext;
        }
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null || string.IsNullOrEmpty(userObj.Username) || string.IsNullOrEmpty(userObj.Password))
            {
                return BadRequest();
            }
            var user = await _authContext.Users.FirstOrDefaultAsync(x => x.Username == userObj.Username && x.Password == userObj.Password);

            if (user == null)
            {
                return NotFound(new { Message = "user not found" });
            }
            user.Token = CreateJWTtoken(user);

            return Ok(new
            {
                Token = user.Token,
                Message = "Login Success!"
            });
        }
        [HttpPost("register")]

        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null || string.IsNullOrEmpty(userObj.Username) || string.IsNullOrEmpty(userObj.Password))
            {
                return BadRequest(new { Message = "Invalid input data" });
            }

            await _authContext.Users.AddAsync(userObj);
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "User Registered!"
            });

        }
        private string CreateJWTtoken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("VERYVERYSECRET.....");
            var identity = new ClaimsIdentity(new Claim[]
        {
             // Adding null checks for user properties
            user.Role != null ? new Claim(ClaimTypes.Role, user.Role) : null,
            user.FirstName != null && user.LastName != null ? new Claim(ClaimTypes.Name, $"{user.Username}") : null
        }.Where(c => c != null));


            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescripter = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescripter);

            return jwtTokenHandler.WriteToken(token);
        }

        private string createRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            var tokenInUser = _authContext.Users.Any(a => a.RefreshToken == refreshToken);
            if (tokenInUser)
            {
                return createRefreshToken();
            }

            return refreshToken;
        }

       

        [Authorize]
        [HttpGet]

        public async Task<ActionResult> GetAllUsers()
        {
            return Ok( await _authContext.Users.ToListAsync());
        }


    }
   

}
