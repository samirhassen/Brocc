using nCustomer.DbModel;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    [RoutePrefix("Api/LegacyCustomerCard")]
    public class LegacyCustomerCardController : NController
    {
        [HttpPost]
        [Route("FetchUiData")]
        public ActionResult FetchUiData(int customerId)
        {
            if (customerId == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            using (var db = new CustomersContext())
            {
                var repo = CreateSearchRepo(db);

                var props = ToUiModel(customerId, repo.GetProperties(customerId, skipDecryptingEncryptedItems: true));

                var isCompany = props.Any(x => x.Name == CustomerProperty.Codes.orgnr.ToString());

                if (!props.Any(x => x.Name == CustomerProperty.Codes.civicRegNr.ToString()) && !isCompany)
                    throw new Exception($"Customer {customerId} is broken. No civicRegNr or orgnr");

                return Json2(new
                {
                    isCompany = isCompany,
                    customerId = customerId,
                    customerCardItems = props
                });
            }
        }

        private class CustomerPropertyViewModel
        {
            public string Name { get; set; }
            public string Group { get; set; }
            public int CustomerId { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; }
            public bool Locked { get; set; }
            public bool IsReadonly { get; set; }
            public string UiType { get; set; } //For validation and such
        }

        private static List<CustomerPropertyViewModel> ToUiModel(int customerId, IEnumerable<CustomerPropertyModel> items)
        {
            var newItems = new Dictionary<string, CustomerPropertyViewModel>();
            foreach (var i in items)
            {
                newItems[i.Name] = new CustomerPropertyViewModel
                {
                    CustomerId = i.CustomerId,
                    Group = i.Group,
                    IsSensitive = i.IsSensitive,
                    Locked = i.IsSensitive,
                    Name = i.Name,
                    Value = i.IsSensitive ? null : i.Value
                };
            }

            Action<CustomerPropertyViewModel> addIfNotExists = i =>
            {
                if (!newItems.ContainsKey(i.Name))
                    newItems[i.Name] = i;
            };

            ///////////////////////////////////////
            /// Sanction //////////////////////////
            /// ///////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.sanction.ToString(),
                    Group = CustomerProperty.Groups.sanction.ToString(),
                    Value = "false",
                    IsSensitive = false,
                    Locked = false
                });

            ///////////////////////////////////////
            /// Pep ///////////////////////////////
            ///////////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.externalIsPep.ToString(),
                    Group = CustomerProperty.Groups.pepKyc.ToString(),
                    Value = "false",
                    IsSensitive = false,
                    Locked = false
                });

            ///////////////////////////////////////
            /// Tax countries  ////////////////////
            ///////////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.taxcountries.ToString(),
                    Group = CustomerProperty.Groups.taxResidency.ToString(),
                    Value = "[]",
                    IsSensitive = false,
                    Locked = false
                });

            //////////////////////////////////////
            /// Name and address /////////////////
            //////////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.firstName.ToString(),
                    Group = CustomerProperty.Groups.insensitive.ToString(),
                    Value = "",
                    IsSensitive = false,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.lastName.ToString(),
                    Group = CustomerProperty.Groups.sensitive.ToString(),
                    Value = "",
                    IsSensitive = true,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.addressStreet.ToString(),
                    Group = CustomerProperty.Groups.sensitive.ToString(),
                    Value = "",
                    IsSensitive = true,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.addressZipcode.ToString(),
                    Group = CustomerProperty.Groups.sensitive.ToString(),
                    Value = "",
                    IsSensitive = true,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.addressCity.ToString(),
                    Group = CustomerProperty.Groups.sensitive.ToString(),
                    Value = "",
                    IsSensitive = true,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.addressCountry.ToString(),
                    Group = CustomerProperty.Groups.sensitive.ToString(),
                    Value = NEnv.ClientCfg.Country.BaseCountry,
                    IsSensitive = true,
                    Locked = false
                });
            //////////////////////////////////////
            /// Contact info /////////////////////
            //////////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.email.ToString(),
                    Group = CustomerProperty.Groups.insensitive.ToString(),
                    Value = "",
                    IsSensitive = false,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.phone.ToString(),
                    Group = CustomerProperty.Groups.insensitive.ToString(),
                    Value = "",
                    IsSensitive = false,
                    Locked = false
                });

            //////////////////////////////////////
            /// FATCA        /////////////////////
            //////////////////////////////////////
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.includeInFatcaExport.ToString(),
                    Group = CustomerProperty.Groups.fatca.ToString(),
                    Value = "false",
                    IsSensitive = false,
                    Locked = false
                });
            addIfNotExists(
                new CustomerPropertyViewModel
                {
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.tin.ToString(),
                    Group = CustomerProperty.Groups.fatca.ToString(),
                    Value = "",
                    IsSensitive = true,
                    Locked = false
                });

            var outItems = new List<CustomerPropertyViewModel>();
            foreach (var i in newItems.Values)
            {
                var code = (CustomerProperty.Codes)Enum.Parse(typeof(CustomerProperty.Codes), i.Name, true);
                var spec = UiSpec.GetForCode(code);

                if (!spec.IsHidden)
                {
                    outItems.Add(new CustomerPropertyViewModel
                    {
                        CustomerId = i.CustomerId,
                        Group = i.Group,
                        IsSensitive = i.IsSensitive,
                        Locked = i.Locked,
                        Name = i.Name,
                        Value = i.Value,
                        IsReadonly = spec.IsReadOnly,
                        UiType = spec.UiType.ToString()
                    });
                }
            }

            return outItems.OrderBy(x => x.Name).ThenBy(x => x.Group).ToList();
        }

        private class UiSpec
        {
            public enum TypeCode
            {
                String,
                Email,
                Date,
                Boolean,
                Custom
            }

            public bool IsReadOnly { get; set; }
            public bool IsHidden { get; set; }
            public TypeCode UiType { get; set; } = TypeCode.String;

            public static UiSpec GetForCode(CustomerProperty.Codes code)
            {
                switch (code)
                {
                    case CustomerProperty.Codes.civicRegNr:
                    case CustomerProperty.Codes.civicregnr_country:
                        return new UiSpec { IsReadOnly = true, UiType = TypeCode.String };

                    case CustomerProperty.Codes.firstName:
                    case CustomerProperty.Codes.lastName:
                    case CustomerProperty.Codes.fullname:
                    case CustomerProperty.Codes.addressStreet:
                    case CustomerProperty.Codes.addressZipcode:
                    case CustomerProperty.Codes.addressCity:
                    case CustomerProperty.Codes.addressCountry:
                    case CustomerProperty.Codes.phone:
                        return new UiSpec { IsReadOnly = false, UiType = TypeCode.String };

                    case CustomerProperty.Codes.email:
                        return new UiSpec { IsReadOnly = false, UiType = TypeCode.Email };

                    case CustomerProperty.Codes.sanction:
                    case CustomerProperty.Codes.externalIsPep:
                        return new UiSpec { IsReadOnly = false, UiType = TypeCode.Boolean };

                    case CustomerProperty.Codes.birthDate:
                        return new UiSpec { IsReadOnly = false, UiType = TypeCode.Date };

                    case CustomerProperty.Codes.externalKycScreeningDate:
                        return new UiSpec { IsReadOnly = true, UiType = TypeCode.Date };

                    case CustomerProperty.Codes.mainoccupation:
                    case CustomerProperty.Codes.mainoccupation_text:
                    case CustomerProperty.Codes.countrycodes:
                    case CustomerProperty.Codes.tin:
                    case CustomerProperty.Codes.includeInFatcaExport:
                    case CustomerProperty.Codes.questions:
                    case CustomerProperty.Codes.commercialInfo:
                    case CustomerProperty.Codes.usemypersonaldata:
                    case CustomerProperty.Codes.usemypersonaldataconsenttext:
                    case CustomerProperty.Codes.ispep:
                    case CustomerProperty.Codes.pep_roles:
                    case CustomerProperty.Codes.pep_name:
                    case CustomerProperty.Codes.pep_text:
                    case CustomerProperty.Codes.taxcountries:
                        return new UiSpec { UiType = TypeCode.Custom };

                    default:
                        return new UiSpec { IsHidden = true };
                }
            }
        }
    }
}