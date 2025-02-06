using System;

namespace NTech.Core.Module.Shared.Services
{
    public class OneTimeTokenGenerator
    {
        private Random random = new Random();
        private object randomLock = new object();

        private OneTimeTokenGenerator()
        {

        }

        private static readonly OneTimeTokenGenerator sharedInstance = new OneTimeTokenGenerator();

        public static OneTimeTokenGenerator SharedInstance
        {
            get
            {
                return sharedInstance;
            }
        }

        public string GenerateUniqueToken(int length = 20)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            lock (randomLock)
            {
                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = Chars[random.Next(Chars.Length)];
                }
            }
            return new string(stringChars);
        }
    }
}
