using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Text.Json;
using SignUpApi.Models;

namespace SignUpApi.Features.Authentication.Signup
{
    public class SignupHandler : IRequestHandler<SignupCommand, SignupResult>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SignupHandler(UserManager<AppUser> userManager, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<SignupResult> Handle(SignupCommand request, CancellationToken cancellationToken)
        {
            // Verify reCAPTCHA token
            var isRecaptchaValid = await VerifyRecaptchaAsync(request.RecaptchaToken, cancellationToken);
            if (!isRecaptchaValid)
            {
                return new SignupResult
                {
                    Success = false,
                    Message = "reCAPTCHA verification failed. Please try again."
                };
            }

            // Create user
            var user = new AppUser
            {
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = false, 
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return new SignupResult
                {
                    Success = true,
                    Message = "User registered successfully",
                    UserId = user.Id
                };
            }
            else
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return new SignupResult
                {
                    Success = false,
                    Message = $"Registration failed: {errorMessage}"
                };
            }
        }

        private async Task<bool> VerifyRecaptchaAsync(string token, CancellationToken cancellationToken)
        {
            var secretKey = _configuration["Recaptcha:SecretKey"] 
                ?? throw new InvalidOperationException("Recaptcha:SecretKey is not configured");

            var client = _httpClientFactory.CreateClient();
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", token)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", formContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            using var jsonDocument = JsonDocument.Parse(responseContent);
            if (jsonDocument.RootElement.TryGetProperty("success", out var successProperty))
            {
                return successProperty.GetBoolean();
            }

            return false;
        }
    }
}