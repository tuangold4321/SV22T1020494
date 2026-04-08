namespace SV22T1020494.Models.Security
{
    public enum AuthenticationStatus
    {
        Success,
        NotFound,
        Locked,
        InvalidPassword
    }

    public class AuthenticationResult
    {
        public AuthenticationStatus Status { get; set; }
        public UserAccount? User { get; set; }
    }
}
