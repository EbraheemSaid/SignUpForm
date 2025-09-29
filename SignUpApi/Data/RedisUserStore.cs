using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using System.Text.Json;
using System.Security.Claims;
using SignUpApi.Models;

namespace SignUpApi.Data
{
    public class RedisUserStore : IUserStore<AppUser>, IUserPasswordStore<AppUser>, IUserEmailStore<AppUser>
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisUserStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = _redis.GetDatabase();
        }

        public void Dispose()
        {
            // No resources to dispose in this implementation
        }

        public async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Check if email already exists (since email must be unique)
            if (!string.IsNullOrEmpty(user.NormalizedEmail))
            {
                var existingUser = await FindByEmailAsync(user.NormalizedEmail, cancellationToken);
                if (existingUser != null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DuplicateEmail",
                        Description = "Email address is already in use."
                    });
                }
            }

            // Generate a unique ID if not already set
            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = Guid.NewGuid().ToString();
            }

            // Store user in Redis as a hash
            var hashFields = new HashEntry[]
            {
                new HashEntry("Id", user.Id),
                new HashEntry("UserName", user.UserName ?? ""),
                new HashEntry("NormalizedUserName", user.NormalizedUserName ?? ""),
                new HashEntry("Email", user.Email ?? ""),
                new HashEntry("NormalizedEmail", user.NormalizedEmail ?? ""),
                new HashEntry("EmailConfirmed", user.EmailConfirmed),
                new HashEntry("PasswordHash", user.PasswordHash ?? ""),
                new HashEntry("SecurityStamp", user.SecurityStamp ?? ""),
                new HashEntry("ConcurrencyStamp", user.ConcurrencyStamp ?? ""),
                new HashEntry("PhoneNumber", user.PhoneNumber ?? ""),
                new HashEntry("PhoneNumberConfirmed", user.PhoneNumberConfirmed),
                new HashEntry("TwoFactorEnabled", user.TwoFactorEnabled),
                new HashEntry("LockoutEnd", user.LockoutEnd?.ToString() ?? ""),
                new HashEntry("LockoutEnabled", user.LockoutEnabled),
                new HashEntry("AccessFailedCount", user.AccessFailedCount),
                new HashEntry("FirstName", user.FirstName ?? ""),
                new HashEntry("LastName", user.LastName ?? ""),
                new HashEntry("CreatedAt", user.CreatedAt.ToString("O"))
            };

            // Store user data in hash
            await _database.HashSetAsync($"User:{user.Id}", hashFields);

            // Create secondary index for email only (since only email needs to be unique)
            if (!string.IsNullOrEmpty(user.NormalizedEmail))
            {
                await _database.StringSetAsync($"User:Email:{user.NormalizedEmail}", user.Id);
            }

            // Note: We don't create a username index, so usernames are not unique

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Only delete email secondary index (since only email is unique)
            var existingUser = await FindByIdAsync(user.Id, cancellationToken);
            if (existingUser != null)
            {
                if (!string.IsNullOrEmpty(existingUser.NormalizedEmail) && 
                    !string.Equals(existingUser.NormalizedEmail, user.NormalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    await _database.KeyDeleteAsync($"User:Email:{existingUser.NormalizedEmail}");
                }
            }

            // Update user in Redis as a hash
            var hashFields = new HashEntry[]
            {
                new HashEntry("Id", user.Id),
                new HashEntry("UserName", user.UserName ?? ""),
                new HashEntry("NormalizedUserName", user.NormalizedUserName ?? ""),
                new HashEntry("Email", user.Email ?? ""),
                new HashEntry("NormalizedEmail", user.NormalizedEmail ?? ""),
                new HashEntry("EmailConfirmed", user.EmailConfirmed),
                new HashEntry("PasswordHash", user.PasswordHash ?? ""),
                new HashEntry("SecurityStamp", user.SecurityStamp ?? ""),
                new HashEntry("ConcurrencyStamp", user.ConcurrencyStamp ?? ""),
                new HashEntry("PhoneNumber", user.PhoneNumber ?? ""),
                new HashEntry("PhoneNumberConfirmed", user.PhoneNumberConfirmed),
                new HashEntry("TwoFactorEnabled", user.TwoFactorEnabled),
                new HashEntry("LockoutEnd", user.LockoutEnd?.ToString() ?? ""),
                new HashEntry("LockoutEnabled", user.LockoutEnabled),
                new HashEntry("AccessFailedCount", user.AccessFailedCount),
                new HashEntry("FirstName", user.FirstName ?? ""),
                new HashEntry("LastName", user.LastName ?? ""),
                new HashEntry("CreatedAt", user.CreatedAt.ToString("O"))
            };

            await _database.HashSetAsync($"User:{user.Id}", hashFields);

            // Only create email secondary index (since only email is unique)
            if (!string.IsNullOrEmpty(user.NormalizedEmail))
            {
                await _database.StringSetAsync($"User:Email:{user.NormalizedEmail}", user.Id);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Remove user data
            await _database.KeyDeleteAsync($"User:{user.Id}");

            // Remove secondary indexes - only email index since we don't maintain username uniqueness
            if (!string.IsNullOrEmpty(user.NormalizedEmail))
            {
                await _database.KeyDeleteAsync($"User:Email:{user.NormalizedEmail}");
            }

            return IdentityResult.Success;
        }

        public async Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userData = await _database.HashGetAllAsync($"User:{userId}");
            if (userData.Length == 0)
            {
                return null;
            }

            return MapHashToUser(userData);
        }

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            // Since we don't maintain username uniqueness, we need to search through all users
            // This is less efficient, but usernames are not unique in this implementation
            // For production, consider using a different approach or making usernames unique too
            
            // For now, return null as we don't support finding by username efficiently
            // The application should use email for lookups instead
            return Task.FromResult<AppUser?>(null);
        }

        // We still need to implement this method to satisfy the interface
        // But we'll make username uniqueness not enforced by returning null here
        // The actual uniqueness check will happen at the application level if needed

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.Id ?? throw new InvalidOperationException("User ID is null"));
        }

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken = default)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.UserName);
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken = default)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken = default)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken = default)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            var userId = await _database.StringGetAsync($"User:Email:{normalizedEmail}");
            if (userId.IsNullOrEmpty)
            {
                return null;
            }

            return await FindByIdAsync(userId!, cancellationToken);
        }

        public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken = default)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken cancellationToken = default)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        private AppUser? MapHashToUser(HashEntry[] hashEntries)
        {
            if (hashEntries.Length == 0) return null;

            var user = new AppUser();
            var properties = typeof(AppUser).GetProperties();

            foreach (var entry in hashEntries)
            {
                switch (entry.Name)
                {
                    case "Id":
                        user.Id = entry.Value!;
                        break;
                    case "UserName":
                        user.UserName = entry.Value;
                        break;
                    case "NormalizedUserName":
                        user.NormalizedUserName = entry.Value;
                        break;
                    case "Email":
                        user.Email = entry.Value;
                        break;
                    case "NormalizedEmail":
                        user.NormalizedEmail = entry.Value;
                        break;
                    case "EmailConfirmed":
                        user.EmailConfirmed = (bool)entry.Value;
                        break;
                    case "PasswordHash":
                        user.PasswordHash = entry.Value;
                        break;
                    case "SecurityStamp":
                        user.SecurityStamp = entry.Value;
                        break;
                    case "ConcurrencyStamp":
                        user.ConcurrencyStamp = entry.Value;
                        break;
                    case "PhoneNumber":
                        user.PhoneNumber = entry.Value;
                        break;
                    case "PhoneNumberConfirmed":
                        user.PhoneNumberConfirmed = (bool)entry.Value;
                        break;
                    case "TwoFactorEnabled":
                        user.TwoFactorEnabled = (bool)entry.Value;
                        break;
                    case "LockoutEnd":
                        if (!string.IsNullOrEmpty(entry.Value))
                        {
                            user.LockoutEnd = DateTimeOffset.Parse(entry.Value!);
                        }
                        break;
                    case "LockoutEnabled":
                        user.LockoutEnabled = (bool)entry.Value;
                        break;
                    case "AccessFailedCount":
                        user.AccessFailedCount = (int)entry.Value;
                        break;
                    case "FirstName":
                        user.FirstName = entry.Value;
                        break;
                    case "LastName":
                        user.LastName = entry.Value;
                        break;
                    case "CreatedAt":
                        if (!string.IsNullOrEmpty(entry.Value))
                        {
                            user.CreatedAt = DateTime.Parse(entry.Value!);
                        }
                        break;
                }
            }

            return user;
        }
    }
}