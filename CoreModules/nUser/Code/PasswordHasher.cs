using System;
using System.Security.Cryptography;
using System.Text;

namespace nUser.Code
{
    public static class PasswordHasher
    {
        private const int Rounds = 10000; //This CAN be safely changed without breaking stored passwords
        private static int SaltSize = 20;//This CAN be safely changed without breaking stored passwords

        private const int HashSize = 20; //This CANNOT be safely changed without breaking stored passwords
        private static Encoding enc = Encoding.UTF8; //This CANNOT be safely changed without breaking stored passwords

        public static bool IsValid(string plainTextPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new ArgumentException("plainTextPassword");
            if (string.IsNullOrWhiteSpace(storedHash))
                throw new ArgumentException("storedHash");

            var parts = storedHash.Split(';');
            var rounds = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var correctHash = parts[2];
            using (var hasher = new Rfc2898DeriveBytes(enc.GetBytes(plainTextPassword), salt, rounds))
            {
                var hash = hasher.GetBytes(HashSize);
                return Convert.ToBase64String(hash) == correctHash;
            }
        }

        public static string Hash(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new ArgumentException("plainTextPassword");

            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[SaltSize];
                rng.GetBytes(salt);

                var rounds = Rounds;
                using (var hasher = new Rfc2898DeriveBytes(enc.GetBytes(plainTextPassword), salt, rounds))
                {
                    var hash = hasher.GetBytes(HashSize);
                    return $"{rounds};{Convert.ToBase64String(salt)};{Convert.ToBase64String(hash)}";
                }
            }
        }
    }
}