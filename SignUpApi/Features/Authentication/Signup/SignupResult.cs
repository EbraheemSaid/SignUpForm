namespace SignUpApi.Features.Authentication.Signup
{
    public class SignupResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }
    }
}