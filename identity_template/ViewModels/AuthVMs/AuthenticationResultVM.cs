namespace identity_template.ViewModels.AuthVMs
{
    public class AuthenticationResultVM
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

}
