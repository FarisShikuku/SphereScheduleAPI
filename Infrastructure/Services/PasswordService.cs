using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace SphereScheduleAPI.Infrastructure.Services
{
    public class PasswordService
    {
        private const int SaltSize = 128 / 8; // 128 bits
        private const int HashSize = 256 / 8; // 256 bits
        private const int Iterations = 10000;

        public (string Hash, string Salt) HashPassword(string password)
        {
            // Generate salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash password
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );

            return (
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt)
            );
        }

        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );

            return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
        }

        public string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[bytes[i] % validChars.Length];
            }

            return new string(chars);
        }

        public bool IsPasswordStrong(string password)
        {
            // At least 8 characters
            if (password.Length < 8) return false;

            // Contains uppercase
            if (!password.Any(char.IsUpper)) return false;

            // Contains lowercase
            if (!password.Any(char.IsLower)) return false;

            // Contains digit
            if (!password.Any(char.IsDigit)) return false;

            // Contains special character
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;

            return true;
        }
    }
}