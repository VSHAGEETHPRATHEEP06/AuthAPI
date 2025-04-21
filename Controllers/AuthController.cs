using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly TokenService _tokenService;
        private readonly UserSessionService _userSessionService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            TokenService tokenService,
            UserSessionService userSessionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _userSessionService = userSessionService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
                {
                    return BadRequest("Email already in use");
                }

                var user = new ApplicationUser
                {
                    Email = registerDto.Email,
                    UserName = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                // Validate the role
                string role = registerDto.Role?.Trim().ToLower() ?? "user";
                
                // Only allow "user" or "admin" roles
                if (role != "user" && role != "admin")
                {
                    role = "user"; // Default to User role if invalid
                }

                // Assign role based on parameter
                await _userManager.AddToRoleAsync(user, role);

                Console.WriteLine($"User {user.Email} registered with role {role}");
                
                return Ok(new UserDto
                {
                    Id = (int)user.Id.GetHashCode(), 
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = role
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email" });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid password" });
                }

                // Check if any user is already logged in
                if (_userSessionService.AnyUserLoggedIn())
                {
                    string userId = user.Id.ToString();
                    
                    // If this is the same user trying to login again
                    if (_userSessionService.IsUserLoggedIn(userId))
                    {
                        return BadRequest(new { success = false, message = "You are already logged in" });
                    }
                    
                    // If it's a different user trying to login
                    return BadRequest(new { success = false, message = "Another user is already logged in. Please wait until they logout." });
                }

                // Return token
                var roles = await _userManager.GetRolesAsync(user);
                var token = await _tokenService.CreateTokenAsync(user, roles);

                // Store user session
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;
                _userSessionService.AddUserSession(user.Id.ToString(), token, expiry);

                Console.WriteLine($"User {user.Email} logged in with role: {string.Join(", ", roles)}");

                return Ok(new UserDto
                {
                    Id = (int)user.Id.GetHashCode(), 
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = token,
                    Role = roles.Count > 0 ? roles[0] : "user"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            try
            {
                // Log out the currently logged-in user
                if (_userSessionService.AnyUserLoggedIn())
                {
                    // Get all active users and log them out
                    if (_userSessionService.LogoutCurrentUser())
                    {
                        Console.WriteLine("Successfully logged out current user");
                        return Ok(new { success = true, message = "Successfully logged out" });
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Failed to log out. Please try again." });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "No user is currently logged in" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Logout: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }
    }
}