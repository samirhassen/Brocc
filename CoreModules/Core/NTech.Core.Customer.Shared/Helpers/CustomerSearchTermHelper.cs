using nCustomer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTech.Core.Customer.Shared.Helpers
{
    public static class CustomerSearchTermHelper
    {
        public static List<string> ComputeEmailSearchTerms(string email)
        {
            string Md5(params string[] input)
            {
                var s = string.Concat(input.Select(x => x?.Trim()?.ToLowerInvariant()));
                using (var md5Hash = System.Security.Cryptography.MD5.Create())
                {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s));
                    return Convert.ToBase64String(data);
                }
            }
            return new List<string> { Md5(email) };
        }

        public static List<string> ComputeNameSearchTerms(string name)
        {
            var generator = new Phonix.DoubleMetaphone();
            return name.Split(new char[0]).Where(y => !string.IsNullOrWhiteSpace(y)).Select(y => generator.BuildKey(y)).ToList();
        }

        public static List<string> ComputePhoneNrSearchTerms(string phoneNr, string clientCountry)
        {
            var phoneNrHandler = PhoneNumberHandler.GetInstance(clientCountry);
            return new List<string> { phoneNrHandler.TryNormalizeToInternationalFormat(phoneNr) };
        }
    }
}
