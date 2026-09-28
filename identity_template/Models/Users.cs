using Microsoft.AspNetCore.Identity;

namespace identity_template.Models
{
    public class Users : IdentityUser
    {
        public DateTime DateOfBirth { get; set; }
    }
}
