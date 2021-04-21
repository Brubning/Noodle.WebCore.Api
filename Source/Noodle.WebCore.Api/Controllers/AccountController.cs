using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noodle.WebCore.Api.Data;
using Noodle.WebCore.Api.Models;

namespace Noodle.WebCore.Api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<PortalUser> _userManager;
        private readonly SignInManager<PortalUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IConfiguration configuration,
            UserManager<PortalUser> userManager,
            SignInManager<PortalUser> signInManager,
            ILogger<AccountController> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost("token")]
        public async Task<ActionResult<string>> GetAuthToken(LoginRequest loginRequest)
        {
            // Check user can sign in
            var result = await _signInManager.PasswordSignInAsync(
                loginRequest.Email,
                loginRequest.Password,
                false,
                false);

            if (result.Succeeded)
            {
                // Get the PortalUser
                var user = await _userManager.FindByNameAsync(loginRequest.Email);
                // Get expiry as seconds since epoch for the payload
                var expiry = (int)(DateTime.UtcNow.AddMinutes(60) - DateTime.UnixEpoch).TotalSeconds;
                // Build token header (See https://jwt.io/)
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ShVmYq3s6v9y$B&E)H@McQfTjWnZr4u7"));
                var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                var header = new JwtHeader(signingCredentials);
                // Build token payload (See https://jwt.io/)
                var payload = new JwtPayload
                {
                    { "sub", "api" },
                    { "name", $"{user.UserName}" },
                    { "exp", expiry },
                    { "iss", "http://localhost" },
                    { "aud", "http://localhost" }
                };
                // Build and return the token as a string
                var token = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(token);

                return Ok(tokenString);
            }

            return BadRequest("Invalid login details");
        }

        [HttpPost("portaluser")]
        public async Task<ActionResult> RegisterUser(RegisterRequest registerRequest)
        {
            var result = await _userManager.CreateAsync(registerRequest.PortalUser, registerRequest.Password);
            
            if (result.Succeeded)
            {
                return Ok();        // Should be CreatedAtRoute
            }

            return BadRequest("Invalid registrationg details");
        }
    }
}
