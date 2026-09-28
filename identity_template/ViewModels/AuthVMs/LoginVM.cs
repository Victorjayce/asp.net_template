using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace identity_template.ViewModels.AuthVMs
{
    public record LoginVM(
        [property: Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Invalid email address.")] string Email,
        [property: Required(ErrorMessage = "Password is required.")] string Password
    );
}
