using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class OneTimeToken : InfrastructureBaseItem
    {
        public string Token { get; set; }
        public string TokenType { get; set; }
        public string TokenExtraData { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset ValidUntilDate { get; set; }
        public DateTimeOffset? RemovedDate { get; set; }
        public int? RemovedBy { get; set; }

        public bool IsExpired()
        {
            return (RemovedBy.HasValue || RemovedDate.HasValue || ValidUntilDate < DateTimeOffset.Now);
        }

        public static string GenerateUniqueToken()
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[20];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }

            return new string(stringChars);
        }
    }
}