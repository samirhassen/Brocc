using NTech.Core.Module.Shared.Database;
using System.Collections.Generic;

namespace nCustomer.DbModel
{
    public class CustomerProperty : InfrastructureBaseItem
    {
        public enum Groups
        {
            civicRegNr,
            official,
            insensitive,
            sensitive,
            pepKyc,
            taxResidency,
            amlCft,
            sanction,
            fatca,
            orgnr
        }

        public enum Codes
        {
            civicRegNr,
            civicregnr_country,
            firstName,
            lastName,
            fullname,
            phone,
            email,
            addressStreet,
            mainoccupation,
            mainoccupation_text,
            sanction,
            addressZipcode,
            addressCity,
            addressCountry,
            addressHash,
            countrycodes,
            taxcountries,
            tin,
            questions,
            commercialInfo,
            usemypersonaldata,
            usemypersonaldataconsenttext,
            externalIsPep,
            ispep,
            pep_roles,
            pep_name,
            pep_text,
            birthDate,
            externalKycScreeningDate,
            wasOnboardedExternally,
            localIsPep,
            localIsSanction,
            includeInFatcaExport,
            hasOtherTaxOrCitizenCountry,
            citizencountries,
            orgnr,
            orgnr_country,
            companyName,
            isCompany,
            sentToCM1,
            snikod,
            amlRiskClass,
            hasOtherTaxCountry,
            hasOtherCitizenCountry
        };

        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public CustomerProperty ReplacesCustomerProperty { get; set; }
        public int? ReplacesCustomerProperty_Id { get; set; }
        public virtual List<CustomerProperty> ReplacedByCustomerProperties { get; set; }
        public bool IsCurrentData { get; set; }
        public bool IsSensitive { get; set; }
        public bool IsEncrypted { get; set; }
        public int? CreatedByBusinessEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
    }
}