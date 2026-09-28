namespace identity_template.ViewModels.AuthVMs
{
    //public class AuthenticationResultVM
    //{
    //    public string Token { get; set; }
    //    public string RefreshToken { get; set; }
    //    public DateTime ExpiresAt { get; set; }

    //    public AuthenticationResultVM(string token, string refreshToken, DateTime expiresAt)
    //    {
    //        Token = token;
    //        RefreshToken = refreshToken;
    //        ExpiresAt = expiresAt;
    //    }
    //}

    public record AuthenticationResultVM(string Token, string RefreshToken, DateTime ExpiresAt);
}
