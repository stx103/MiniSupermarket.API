using Microsoft.AspNetCore.Identity;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Security
{
    public static class UserPasswords
    {
        private static readonly PasswordHasher<User> Hasher = new();

        public static string Hash(User user, string password) => Hasher.HashPassword(user, password);

        public static bool Verify(User user, string password)
        {
            // Existing course databases store plain text; newly created/reset passwords use Identity V3 hashes.
            if (!user.Password.StartsWith("AQAAAA", StringComparison.Ordinal)) return password == user.Password;
            try
            {
                return Hasher.VerifyHashedPassword(user, user.Password, password)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { return false; }
        }
    }
}
