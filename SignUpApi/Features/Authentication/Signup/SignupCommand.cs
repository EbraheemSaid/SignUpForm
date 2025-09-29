using MediatR;

namespace SignUpApi.Features.Authentication.Signup
{
    public class SignupCommand : IRequest<SignupResult>
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}