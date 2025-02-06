using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CreditApplicationOneTimeToken : InfrastructureBaseItem
    {
        public string Token { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public string TokenType { get; set; }
        public string TokenExtraData { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset ValidUntilDate { get; set; }
        public DateTimeOffset? RemovedDate { get; set; }
        public int? RemovedBy { get; set; }
        /*
        public bool IsExpired(IClock clock)
        {
            return (RemovedBy.HasValue || RemovedDate.HasValue || ValidUntilDate < clock.Now);
        }
        */
        private static object tokenRandomLock = new object();
        private static Lazy<Random> tokenRandom = new Lazy<Random>();
        private static int TokenRandomNext(int maxValue)
        {
            lock (tokenRandomLock)
            {
                return tokenRandom.Value.Next(maxValue);
            }
        }

        public static string GenerateUniqueToken()
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[20];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[TokenRandomNext(Chars.Length)];
            }

            return new string(stringChars);
        }
    }
}