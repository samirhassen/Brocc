using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class OcrNumbersTests
    {
        [TestMethod]
        public void SE()
        {
            var validOcr = OcrPaymentReferenceGenerator.GenerateFromSequenceNumber("SE", 42);

            var lastDigit = (int.Parse(new string(new[] { validOcr.NormalForm.Last() })) + 1) % 10;
            var invalidPlausibleOcr = validOcr.NormalForm.Substring(0, validOcr.NormalForm.Length - 1) + lastDigit.ToString();

            var invalidImplausibleOcr = "T344daa23";

            var parser = new OcrNumberParser("SE");

            IOcrNumber o1; string m1;
            Assert.IsTrue(parser.TryParse(validOcr.NormalForm, out o1, out m1));

            IOcrNumber o2; string m2;
            Assert.IsFalse(parser.TryParse(invalidPlausibleOcr, out o2, out m2));

            IOcrNumber o3; string m3;
            Assert.IsFalse(parser.TryParse(invalidImplausibleOcr, out o3, out m3));
        }

        [TestMethod]
        public void FI()
        {
            var validOcr = OcrPaymentReferenceGenerator.GenerateFromSequenceNumber("FI", 42);

            var lastDigit = (int.Parse(new string(new[] { validOcr.NormalForm.Last() })) + 1) % 10;
            var invalidPlausibleOcr = validOcr.NormalForm.Substring(0, validOcr.NormalForm.Length - 1) + lastDigit.ToString();

            var invalidImplausibleOcr = "T344daa23";

            var parser = new OcrNumberParser("FI");

            IOcrNumber o1; string m1;
            Assert.IsTrue(parser.TryParse(validOcr.NormalForm, out o1, out m1));

            IOcrNumber o2; string m2;
            Assert.IsFalse(parser.TryParse(invalidPlausibleOcr, out o2, out m2));

            IOcrNumber o3; string m3;
            Assert.IsFalse(parser.TryParse(invalidImplausibleOcr, out o3, out m3));
        }
    }
}
