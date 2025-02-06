using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure;
using System.Collections.Generic;

namespace TestsnPreCredit
{
    [TestClass]
    public class NTechUserNameValidatorTests
    {
        [TestMethod]
        public void DisplayNames()
        {
            RunTest(NTechUserNameValidator.UserNameTypeCode.DisplayUserName,
                new HashSet<string> { "Karl", "Karl Anka", "Kärl Anka", "Karl1", "Karl Anka 1", "Karl-Anka", "Ärling" },
                new HashSet<string> { "1Karl", "Karl;", "-Karl Anka", "Test test2</script><img src=1 onerror=alert(document.domain)>", "Karl<" });
        }

        [TestMethod]
        public void ActiveDirectoryNames()
        {
            RunTest(NTechUserNameValidator.UserNameTypeCode.ActiveDirectoryUserName,
                new HashSet<string> { "Karl", "Karl Anka", "Kärl Anka", "Karl1", "Karl Anka 1", "Karl-Anka", "Ärling", @"domain.local\Karl-Änka" },
                new HashSet<string> { "1Karl", "Karl;", "-Karl Anka", "Test test2</script><img src=1 onerror=alert(document.domain)>", "Karl<", @"\domain.localKarl-Änka", @"domain.local\Karl\Änka" });
        }

        [TestMethod]
        public void EmailAccountNames()
        {
            RunTest(NTechUserNameValidator.UserNameTypeCode.EmailUserName,
                new HashSet<string> { "test@example.org" },
                new HashSet<string> { "testexample.org", "Test test2</script><img src=1 onerror=alert(document.domain)>" });
        }

        private void RunTest(NTechUserNameValidator.UserNameTypeCode code, ISet<string> valid, ISet<string> invalid)
        {
            var v = new NTechUserNameValidator();
            foreach (var validName in valid)
            {
                string msg = "";
                Assert.IsTrue(v.IsValidUserName(validName, code, observeInvalidMessage: x => msg = x), $"Should be valid '{validName}' ({msg})");
            }

            foreach (var invalidName in invalid)
            {
                string msg = "";
                Assert.IsFalse(v.IsValidUserName(invalidName, code, observeInvalidMessage: x => msg = x), $"Should be invalid '{invalidName}' ({msg})");
            }
        }
    }
}