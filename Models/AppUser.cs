using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TravelAPI.Models
{
    public class AppUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; }
        public bool IsDelete { get; set; } = false;

        [MaxLength(250)]
        public string? Avatar { get; set; }
    }
    public class Login
    {
        public string UserName { get; set; }
        public string Password { get; set; }

    }
}
