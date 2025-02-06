using NTech.Services.Infrastructure.Email;
using System;
using System.Linq;

namespace NTech.Services.Infrastructure
{
    public class NTechUserNameValidator
    {
        public bool TryValidateUserName(string name, UserNameTypeCode typeCode, out string invalidMessage)
        {
            invalidMessage = null;
            string msg = null;
            if (!IsValidUserName(name, typeCode, observeInvalidMessage: x => msg = x))
            {
                invalidMessage = msg;
                return false;
            }
            return true;
        }

        public bool IsValidUserName(string name, UserNameTypeCode typeCode, Action<string> observeInvalidMessage = null)
        {
            if (typeCode == UserNameTypeCode.EmailUserName)
                return IsValidEmailUserName(name, observeInvalidMessage: observeInvalidMessage);

            name = name?.Trim() ?? "";
            if (name.Length < 3)
            {
                observeInvalidMessage?.Invoke("Invalid length");
                return false;
            }
            if (!Char.IsLetter(name[0]))
            {
                observeInvalidMessage?.Invoke("Must start with a letter");
                return false;
            }

            if (typeCode == UserNameTypeCode.DisplayUserName)
            {
                if (!name.All(x => Char.IsLetterOrDigit(x) || x == '-' || x == ' '))
                {
                    observeInvalidMessage?.Invoke("Can only contain letters, digits, space or -");
                    return false;
                }
            }
            else if (typeCode == UserNameTypeCode.ActiveDirectoryUserName)
            {
                var suffix = name.Substring(1);
                if (!suffix.All(x => Char.IsLetterOrDigit(x) || x == '-' || x == ' ' || x == '.' || x == '\\'))
                {
                    observeInvalidMessage?.Invoke(@"Can only contain letters, digits, space, ., - or exactly one non leading \");
                    return false;
                }
                if (suffix.Count(x => x == '\\') > 1)
                {
                    observeInvalidMessage?.Invoke(@"Can at most contain one \");
                    return false;
                }
            }
            return true;
        }

        private bool IsValidEmailUserName(string name, Action<string> observeInvalidMessage = null)
        {
            name = name?.Trim() ?? "";
            if (name.Length < 3)
            {
                observeInvalidMessage?.Invoke("Invalid length");
                return false;
            }

            if (!SmtpEmailSender.TryParseMailAddress(name, out var _))
            {
                observeInvalidMessage?.Invoke("Invalid email address");
                return false;
            }

            return true;
        }

        public enum UserNameTypeCode

        {
            DisplayUserName,
            ActiveDirectoryUserName,
            EmailUserName
        }
    }
}