using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCreditReport.Code.SatFi;
using System;
using System.IO;
using System.Text;

namespace TestsnPreCredit
{
    [TestClass]
    public class SatFiParserTests
    {
        [TestMethod]
        public void ResponseParserSmokeTest()
        {
            var exampleResponse = GetExampleXmlResponse();
            var r = SatFiService.ParseResponse(exampleResponse);
            Assert.AreEqual(5, r.CountLoans);
        }

        private static string GetExampleXmlResponse()
        {
            var z = new ZipFile(new MemoryStream(Convert.FromBase64String(SampleResponse)));
            using (var sr = new StreamReader(z.GetInputStream(z.GetEntry("satfi-response-example.xml")), Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        //Base64 encoded zip file of an xml response
        private const string SampleResponse = "UEsDBBQAAAAIAIs7C1Fobo1GkQQAAEIiAAAaAAAAc2F0ZmktcmVzcG9uc2UtZXhhbXBsZS54bWztWt2Ok0AUvjfxHYhXGm2Z4a/QKElLaTSuidmq0atmhNntCMwgM3Tt++wz+AJ9MQeWLmotdNnN2iZLNg2cOeebwzffOZBhX3KG0qFPlzhmKVZ+JDHlw8L26slCiHSoqjxY4ATxvhwq7H2WnavFiYqrIPWJ+/iRIo8rrNcYhTirTKWZcn3IcZBnRKyuZrgGv7i46CNOUIS4IFiw/hlRP787mZVzqh6jPE9wNj/F33PMxdycA9D/wcMnEr0+qqwph2BP4NnowzxgScJoExrcE21KKKIBQfFMIIETTIXHkhTR1dzoA6NhBm1fHspU2+5eb0QLKi5LFHUHhHED/jYr2pCRecN1brtDa2+8iv45MBvgBjeiv20x7Rtpr7rhmKEmCTp7Yr5naR4jQRh9Q89YlpSndcKbYqwL0hh+5DijKMEfWIRp5bDTye33+y/V3y27It4jzi9YFm4iasvfSagNWdR4giRYllWSuhqADtA1Ezi6pUEAnoOrowSq/f4FIkkKolmeuHAATMsam2AC7LE2njhgok9s37SdwcjQx56v+Zo3sCzo27plA0+zfWA6E31qeJORbU4HJjSMKRiY2lTzHHPkj0emY5jjke9Z2kSTv8bYcabeeDQCvrTLQMMZOxPfl74DzSpzrdOpUy0G6ia56afqVkOteuyYhau/O+w5Fps6OmGISvwEZatTzFNpxQ9996HvPvTd/953zWFWFWQ1tjW+SfF0228n1h8dYoe3NowRPc/ROfZYiN3pm5fqlrEZYKvHNjqHSOCyZ/eA3tNMOVtlrIJbZ3IhHOrWsGialakxQbU9w3qCDXOFs8i5C2T4lrUNQ5ZhhmmwKqnzP55KjD+tO9dObVy8bUlkOCSiaujNWdnDFGdcyjF0IQCGDXuaoU+KCeuRNoSA5VS4csXqi9aI+rlT+e7tf8ou6pC2MElpAK8zC/G+kUsU59g1ZGB1sXekwD+Ee5IzwYRAS4KUVdF7I07Wl8rTKI2fSczKre3O1dZbv3u2FtDozJbema8ZzjOElogqUZ5HKOcEZ+tLqvBCwspTOcoOnLcQwq686V1Z+4QkWyEWgiVSZ3HO5Nk3dAwqC6HWlS1o6ga8U8aOQ2C4u8DALemKY8KPTGAYagfD1hGI66y7uDrX4luWCUF6ikBEiUlCiCAldevLo1LaWXelObaj3T17x6Y8vbPyAHDMO+ZPYLrhT54lKOJy7GpckKMgdAHNey/ld6v1T0qxEKtv1/qT8oNaFL1QDr+EF9C694fFSVWkL5RvjHCOlNVCYMKXiIs8l0qkUn5LHKOr02NohFH3Z4gGoN0DZg92bocfc042lask13JUnqbL5OB5027B26AHLPnX/Z2FLohUGiMJ4hyxGC/RcdIYgM7yMyA04S0LGVMlXV+uL1mCDp2n+38z/hITRQcFQWRZkFQ+WFGCJHHyp9QbE4dOm/5faLOOnbbO20k2dDq/j0iecLH9hv7YT7ozqtr92zZAcyqTIzH6GuMZzpYkwNxjORWFWJrGGzaIW7d9pVfL94LaKasH64H2j4b1d8jfPjqWV/W/jLi/AFBLAQI/ABQAAAAIAIs7C1Fobo1GkQQAAEIiAAAaACQAAAAAAAAAIAAAAAAAAABzYXRmaS1yZXNwb25zZS1leGFtcGxlLnhtbAoAIAAAAAAAAQAYAALxATqgb9YBAvEBOqBv1gH7xsU5oG/WAVBLBQYAAAAAAQABAGwAAADJBAAAAAA=";
    }
}