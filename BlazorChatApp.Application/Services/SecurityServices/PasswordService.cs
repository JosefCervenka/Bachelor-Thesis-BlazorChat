namespace BlazorChatApp.Application.Services.SecurityServices
{
    using System;
    using System.Security.Cryptography;

    public class PasswordService
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public byte[] HashPassword(string password, out string salt)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            salt = Convert.ToBase64String(saltBytes);

            byte[] hash = PBKDF2(password, saltBytes);
            return hash;
        }

        public bool VerifyPassword(string password, string salt, byte[] hashedPassword)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] computedHash = PBKDF2(password, saltBytes);

            return CryptographicOperations.FixedTimeEquals(computedHash, hashedPassword);
        }

        private static byte[] PBKDF2(string password, byte[] saltBytes)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: saltBytes,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: KeySize
            );
        }
    }
}
