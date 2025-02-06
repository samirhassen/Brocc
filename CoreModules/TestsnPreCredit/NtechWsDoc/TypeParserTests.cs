using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure.NTechWsDoc;
using System;
using System.Linq;

namespace TestsnPreCredit.NtechWsDoc
{
    [TestClass]
    public class TypeParserTests
    {
        [TestMethod]
        public void SinglePrimitive()
        {
            AssertSinglePrimitive(new { x = (string)null }, "x", PrimitiveTypeCode.String, false, false);
            AssertSinglePrimitive(new { x = (string[])null }, "x", PrimitiveTypeCode.String, false, true);

            AssertSinglePrimitive(new { x = default(int) }, "x", PrimitiveTypeCode.Int, false, false);
            AssertSinglePrimitive(new { x = default(System.Int32) }, "x", PrimitiveTypeCode.Int, false, false);
            AssertSinglePrimitive(new { x = (int?)null }, "x", PrimitiveTypeCode.Int, true, false);
            AssertSinglePrimitive(new { x = (int[])null }, "x", PrimitiveTypeCode.Int, false, true);
            AssertSinglePrimitive(new { x = (int?[])null }, "x", PrimitiveTypeCode.Int, true, true);

            AssertSinglePrimitive(new { x = default(DateTime) }, "x", PrimitiveTypeCode.Date, false, false);
            AssertSinglePrimitive(new { x = (DateTime?)null }, "x", PrimitiveTypeCode.Date, true, false);
            AssertSinglePrimitive(new { x = (DateTime[])null }, "x", PrimitiveTypeCode.Date, false, true);

            AssertSinglePrimitive(new { x = default(DateTimeOffset) }, "x", PrimitiveTypeCode.Date, false, false);
            AssertSinglePrimitive(new { x = (DateTimeOffset?)null }, "x", PrimitiveTypeCode.Date, true, false);
            AssertSinglePrimitive(new { x = (DateTimeOffset[])null }, "x", PrimitiveTypeCode.Date, false, true);

            AssertSinglePrimitive(new { x = default(decimal) }, "x", PrimitiveTypeCode.Decimal, false, false);
            AssertSinglePrimitive(new { x = (decimal?)null }, "x", PrimitiveTypeCode.Decimal, true, false);
            AssertSinglePrimitive(new { x = (decimal[])null }, "x", PrimitiveTypeCode.Decimal, false, true);

            AssertSinglePrimitive(new { x = default(bool) }, "x", PrimitiveTypeCode.Boolean, false, false);
            AssertSinglePrimitive(new { x = (bool?)null }, "x", PrimitiveTypeCode.Boolean, true, false);
            AssertSinglePrimitive(new { x = (bool[])null }, "x", PrimitiveTypeCode.Boolean, false, true);
        }

        [TestMethod]
        public void SingleCompound()
        {
            var templateObject = new
            {
                items = new[]
                {
                    new
                    {
                        x1 = (string)null,
                        x2 = (int?[])null
                    }
                }
            };

            var ct = new TypeParser().ParseType(templateObject.GetType(), "Request");

            Assert.AreEqual(0, ct.PrimtiveProperties.Count);
            Assert.AreEqual(1, ct.CompoundProperties.Count);
            var p = ct.CompoundProperties.Single();
            Assert.AreEqual("items", p.Name);
            Assert.AreEqual(true, p.IsArray);
            Assert.AreEqual(2, p.Type.PrimtiveProperties.Count);
            Assert.AreEqual(0, p.Type.CompoundProperties.Count);
        }

        private void AssertSinglePrimitive<T>(T templateObject, string name, PrimitiveTypeCode code, bool isNullable, bool isArray)
        {
            var parser = new TypeParser();
            var t = parser.ParseType(typeof(T), "Request");

            Assert.AreEqual(0, t.CompoundProperties.Count, GetDesc(t));
            Assert.AreEqual(1, t.PrimtiveProperties.Count, GetDesc(t));

            var p = t.PrimtiveProperties.Single();
            Assert.AreEqual(name, p.Name, GetDesc(t));
            Assert.AreEqual(code.ToString(), p.TypeCode, "Code: " + GetDesc(t));
            Assert.AreEqual(isNullable, p.IsNullable, "Nullable: " + GetDesc(t));
            Assert.AreEqual(isArray, p.IsArray, "Array: " + GetDesc(t));
        }

        private string GetDesc(CompoundType t)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented);
        }

        private class DecimalOnlyClass1
        {
            public decimal Value { get; set; }
        }
        private class DecimalOnlyClass2
        {
            public Decimal Value { get; set; }
        }
        private class NullableDecimalOnlyClass
        {
            public decimal? Value { get; set; }
        }
    }
}
