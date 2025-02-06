using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class ObjectValidationTests
    {
        public class Kitten
        {
            public string A1 { get; set; }

            [Required]
            public string A2 { get; set; }

            [Required]
            public decimal? D2 { get; set; }

            public List<SubItem> Items { get; set; }

            [Required]
            public List<SubItem> Items2 { get; set; }

            [Required]
            public SubItem Item2 { get; set; }

            public class SubItem
            {
                public string Bar1 { get; set; }

                [Required]
                public string Bar2 { get; set; }
            }
        }

        [TestMethod]
        public void TestWebserviceRequestValidation()
        {
            var r = new NTechWebserviceRequestValidator();
            var errors = r.Validate(new Kitten
            {
                Items2 = new List<Kitten.SubItem>
                {
                    new Kitten.SubItem { Bar2 = "a" },
                    new Kitten.SubItem {  },
                    new Kitten.SubItem { Bar2 = "b" },
                    new Kitten.SubItem {  },
                },
                Item2 = new Kitten.SubItem
                {

                }
            });

            Assert.AreEqual(4, errors.Count);
            Assert.AreEqual(1, errors.Count(x => x.Path == "A2"));
            Assert.AreEqual(1, errors.Count(x => x.Path == "D2"));
            Assert.AreEqual(1, errors.Count(x => x.Path == "Item2.Bar2"));

            var e = errors.SingleOrDefault(x => x.Path == "Items2.Bar2");
            Assert.IsNotNull(e);
            Assert.AreEqual(2, e.ListErrorCount);
            Assert.AreEqual(1, e.ListFirstErrorIndex);
        }
    }
}
