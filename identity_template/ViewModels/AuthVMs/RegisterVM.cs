using System.ComponentModel.DataAnnotations;

namespace identity_template.ViewModels.AuthVMs
{
    //public class RegisterVM
    //{
    //    [Required(ErrorMessage = "Username is required.")]
    //    public string UserName { get; set; }
    //    [Required(ErrorMessage = "Email is required.")]
    //    public string Email { get; set; }
    //    [Required(ErrorMessage = "Password is required.")]
    //    public string Password { get; set; }
    //}

    public record RegisterVM(
        [property: Required(ErrorMessage = "Username is required.")] string UserName,
        [property: Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Invalid email address.")] string Email,
        [property: Required(ErrorMessage = "Password is required.")] string Password
    );
}
