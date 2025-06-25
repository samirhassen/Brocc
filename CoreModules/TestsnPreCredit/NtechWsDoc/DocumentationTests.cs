using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.WebserviceMethods.MortgageLoans;
using NTech.Services.Infrastructure.NTechWsDoc;

namespace TestsnPreCredit.NtechWsDoc
{
    [TestClass]
    public class DocumentationTests
    {
        [TestMethod]
        public void Minimal()
        {
            var g = new ServiceMethodDocumentationGenerator();
            var d = g.Generate("a/b/c", "POST", typeof(SetMortgageLoanWorkflowStatusMethod.Request),
                typeof(SetMortgageLoanWorkflowStatusMethod.Response));

            Assert.IsNotNull(d);
        }
    }
}