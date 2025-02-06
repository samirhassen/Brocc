using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Text;

namespace TestsnPreCredit
{
    [TestClass]
    public class AuthorizationHeaderTests
    {
        [TestMethod]
        public void HandlesNull()
        {
            Assert.IsFalse(AuthorizationHeader.TryParseHeader(null, out _));
        }

        [TestMethod]
        public void HandlesValidBearerToken()
        {
            var isOk = AuthorizationHeader.TryParseHeader(" bearer abc123 ", out var header);

            Assert.AreEqual(AuthorizationHeader.HeaderTypeCode.Bearer, header.HeaderType);
            Assert.AreEqual("abc123", header.BearerToken);
            Assert.AreEqual(true, isOk);
        }

        [TestMethod]
        public void HandlesValidBasicAuth()
        {
            var basicAuthValue = Convert.ToBase64String(Encoding.ASCII.GetBytes("un:pw"));
            var isOk = AuthorizationHeader.TryParseHeader($"basic {basicAuthValue}", out var header);

            Assert.AreEqual(AuthorizationHeader.HeaderTypeCode.Basic, header.HeaderType);
            Assert.AreEqual("un", header.BasicAuthUserName);
            Assert.AreEqual("pw", header.BasicAuthPassword);
            Assert.AreEqual(true, isOk);
        }

        [TestMethod]
        public void HandlesRandomOtherTokenType()
        {
            Assert.IsFalse(AuthorizationHeader.TryParseHeader("foo test", out _));
        }
    }
}
