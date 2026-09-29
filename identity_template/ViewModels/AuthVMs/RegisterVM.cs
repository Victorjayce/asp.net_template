using System.ComponentModel.DataAnnotations;

namespace identity_template.ViewModels.AuthVMs
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
