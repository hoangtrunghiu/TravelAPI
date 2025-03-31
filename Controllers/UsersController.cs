using TravelAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace TravelAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet]
        // [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            // Get all non-deleted users
            var users = await _userManager.Users
                .Where(u => !u.IsDelete)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet]
        [Route("by-role/{roleName}")]
        // [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsersByRole(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest($"Role '{roleName}' does not exist.");
            }

            var users = await _userManager.GetUsersInRoleAsync(roleName);

            // Filter out deleted users
            var activeUsers = users.Where(u => !u.IsDelete).ToList();

            return Ok(activeUsers);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userName = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null || user.IsDelete)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userData = new
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Avatar = user.Avatar,
                PhoneNumber = user.PhoneNumber,
                Roles = roles
            };

            return Ok(userData);
        }

        [HttpGet("{id}")]
        // [Authorize(Roles = "Administrator,Editor")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.IsDelete)
            {
                return NotFound($"User with Id {id} not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userData = new
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Avatar = user.Avatar,
                PhoneNumber = user.PhoneNumber,
                Roles = roles
            };

            return Ok(userData);
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] Login loginModel)
        {
            // Set the username to email for consistency
            var user = await _userManager.FindByEmailAsync(loginModel.UserName);

            if (user == null || user.IsDelete)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    fullName = user.FullName,
                    avatar = user.Avatar,
                    roles = userRoles
                });
            }

            return Unauthorized(new { message = "Invalid credentials" });
        }

        [HttpPost]
        [Route("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] Login loginModel)
        {
            var user = await _userManager.FindByEmailAsync(loginModel.UserName);

            if (user == null || user.IsDelete)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                // Check if user has administrative roles
                if (!userRoles.Any(r => r == "Administrator" || r == "Editor"))
                {
                    return Unauthorized(new { message = "Insufficient permissions" });
                }

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    fullName = user.FullName,
                    avatar = user.Avatar,
                    roles = userRoles
                });
            }

            return Unauthorized(new { message = "Invalid credentials" });
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Check if user already exists
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return BadRequest(new { message = "User already exists" });
            }

            // Create new user with email as username
            AppUser user = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = model.Email,
                UserName = model.Email, // Set username to email
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Avatar = string.IsNullOrWhiteSpace(model.Avatar) ? "default-avatar.png" : model.Avatar
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { Errors = errors });
            }

            // Add user to 'User' role if it exists
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }
            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { message = "User created successfully" });
        }

        [HttpPost]
        [Route("register-admin")]
        // [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
        {
            // Check if user already exists
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return BadRequest(new { message = "User already exists" });
            }

            // Create new user with email as username
            AppUser user = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = model.Email,
                UserName = model.Email, // Set username to email
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Avatar = string.IsNullOrWhiteSpace(model.Avatar) ? "default-avatar.png" : model.Avatar
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { Errors = errors });
            }

            // Create Administrator role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync("Administrator"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Administrator"));
            }
            await _userManager.AddToRoleAsync(user, "Administrator");

            return Ok(new { message = "Admin user created successfully" });
        }

        [HttpPost]
        [Route("register-editor")]
        // [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RegisterEditor([FromBody] RegisterModel model)
        {
            // Check if user already exists
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return BadRequest(new { message = "User already exists" });
            }

            // Create new user with email as username
            AppUser user = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = model.Email,
                UserName = model.Email, // Set username to email
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Avatar = string.IsNullOrWhiteSpace(model.Avatar) ? "default-avatar.png" : model.Avatar
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { Errors = errors });
            }

            // Create Editor role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync("Editor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Editor"));
            }
            await _userManager.AddToRoleAsync(user, "Editor");

            return Ok(new { message = "Editor user created successfully" });
        }

        [HttpPut("soft-delete/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> SoftDeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsDelete = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "User deactivated successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPut("restore/{id}")]
        // [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RestoreUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsDelete = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "User restored successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userName = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null || user.IsDelete)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update user properties
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrEmpty(model.Avatar))
            {
                user.Avatar = model.Avatar;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Profile updated successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var userName = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null || user.IsDelete)
            {
                return NotFound(new { message = "User not found" });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { message = "Password changed successfully" });
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { Errors = errors });
        }
    }

    // Additional model classes for request data
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Avatar { get; set; }
    }

    public class UpdateProfileModel
    {
        [Required]
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Avatar { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}