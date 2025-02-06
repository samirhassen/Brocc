using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace TestsnPreCredit.NtechWsDoc
{
    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        public void StringValidationAttribute_ErrorOnInvalidValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new IsExactlyFooRequest
            {
                FooValue = "Bar"
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void StringValidationAttribute_NoErrorOnValidValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new IsExactlyFooRequest
            {
                FooValue = "Foo"
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void StringValidationAttribute_ErrorOnInvalidNestedValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new IsExactlyFooRequest
            {
                Nested = new IsExactlyFooRequest.NestedModel
                {
                    FooValueNested = "Bar"
                }
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void StringValidationAttribute_NoErrorOnMissingValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new IsExactlyFooRequest
            {
                FooValue = null
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ErrorOnSimpleNull()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestSimple
            {
                FooValue = null
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ValidOnSimplePresent()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestSimple
            {
                FooValue = "Ok"
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ErrorOnNestedNullSimple()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestNestedSimple
            {
                Nested = null
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ErrorOnNestedNullComplex()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestNestedComplex
            {
                Nested = new RequiredRequestNestedComplex.NestedModel
                {
                    FooValueNested = null
                }
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ValidOnNestedComplexContainerNull()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestNestedComplex
            {
                Nested = null
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ErrorOnNestedComplexHasIncorrectValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestNestedComplex
            {
                Nested = new RequiredRequestNestedComplex.NestedModel
                {
                    FooValueNested = "Bar"
                }
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void RequiredAttribute_ValidOnNestedComplexHasValue()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new RequiredRequestNestedComplex
            {
                Nested = new RequiredRequestNestedComplex.NestedModel
                {
                    FooValueNested = "Foo"
                }
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void EnumCodeAttribute_ValidOnDefinedByEnum()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new EnumRequest
            {
                FooValue = "a"
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(0, errors.Count, JsonConvert.SerializeObject(errors));
        }

        [TestMethod]
        public void EnumCodeAttribute_ErrorIfNotDefinedByEnum()
        {
            var validator = new NTechWebserviceRequestValidator();
            var r1 = new EnumRequest
            {
                FooValue = "d"
            };
            var errors = validator.Validate(r1);

            Assert.AreEqual(1, errors.Count, JsonConvert.SerializeObject(errors));
        }
    }

    public class IsExactlyFooRequest
    {
        [IsExactlyFoo]
        public string FooValue { get; set; }

        public NestedModel Nested { get; set; }

        public class NestedModel
        {
            [IsExactlyFoo]
            public string FooValueNested { get; set; }
        }
    }

    public class RequiredRequestSimple
    {
        [Required]
        public string FooValue { get; set; }
    }

    public class RequiredRequestNestedSimple
    {
        [Required]
        public NestedModel Nested { get; set; }

        public class NestedModel
        {

        }
    }

    public class RequiredRequestNestedComplex
    {
        public NestedModel Nested { get; set; }

        public class NestedModel
        {
            [IsExactlyFoo]
            [Required]
            public string FooValueNested { get; set; }
        }
    }

    public class EnumRequest
    {
        public enum FooCode
        {
            a, b, c
        }
        [EnumCode(EnumType = typeof(FooCode))]
        public string FooValue { get; set; }
    }

    public class IsExactlyFooAttribute : NTechWsStringValidationAttributeBase
    {
        protected override bool IsValidString(string value) => value == "Foo";
    }
}
