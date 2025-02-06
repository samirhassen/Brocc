using Dapper;
using nCustomer.Code.Services;
using nCustomer.DbModel;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Helpers;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer
{
    public static class CustomerSearchTerms
    {
        public static List<string> TranslateSearchTermValue(string term, string value, IClientConfigurationCore clientConfiguration)
        {
            if (term == SearchTermCode.email.ToString())
            {
                return CustomerSearchTermHelper.ComputeEmailSearchTerms(value);
            }
            else if (term == SearchTermCode.firstName.ToString() || term == SearchTermCode.lastName.ToString())
            {
                return CustomerSearchTermHelper.ComputeNameSearchTerms(value);
            }
            else if (term == SearchTermCode.phone.ToString())
            {
                return CustomerSearchTermHelper.ComputePhoneNrSearchTerms(value, clientConfiguration.Country.BaseCountry);
            }
            else
                throw new NotImplementedException();
        }

        public static void OnCustomerPropertiesAddedShared(ICustomerContext db, INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration, params SearchTermUpdateItem[] items)
        {
            foreach (var propertyGroup in items.GroupBy(x => x.PropertyName))
            {
                SearchTermCode? term = null;
                if (propertyGroup.Key == CustomerProperty.Codes.email.ToString())
                {
                    term = SearchTermCode.email;
                }
                else if (propertyGroup.Key == CustomerProperty.Codes.firstName.ToString())
                {
                    term = SearchTermCode.firstName;
                }
                else if (propertyGroup.Key == CustomerProperty.Codes.lastName.ToString())
                {
                    term = SearchTermCode.lastName;
                }
                else if (propertyGroup.Key == CustomerProperty.Codes.phone.ToString())
                {
                    term = SearchTermCode.phone;
                }
                else if (propertyGroup.Key == CustomerProperty.Codes.companyName.ToString())
                {
                    CompanyLoanSearchTerms.PopulateSearchTermsGroupComposable(currentUser, propertyGroup.Select(x => Tuple.Create(x.CustomerId, x.ClearTextValue)), db, clock);
                }

                if (term.HasValue)
                {
                    var now = clock.Now;

                    //For performance reasons we do a naked update here to avoid having to do a select first since this can be run in bulk. This requires an ambient transaction to be safe
                    var customerIds = propertyGroup.Select(x => x.CustomerId).Distinct().ToList();
                    var query = $"update dbo.CustomerSearchTerm set IsActive = 0, ChangedById = @userId, ChangedDate = @changedDate where TermCode = @termCode and CustomerId in (@customerIds)";
                    db.GetConnection().Execute(query, param: new
                    {
                        userId = currentUser.UserId,
                        changedDate = clock.Now,
                        termCode = term.Value.ToString(),
                        customerIds
                    },
                    transaction: db.CurrentTransaction);

                    foreach (var item in propertyGroup)
                    {
                        var values = TranslateSearchTermValue(term.Value.ToString(), item.ClearTextValue, clientConfiguration);
                        db.AddCustomerSearchTerms(values
                            .Select(value => new CustomerSearchTerm
                            {
                                ChangedById = currentUser.UserId,
                                ChangedDate = now,
                                CustomerId = item.CustomerId,
                                IsActive = true,
                                InformationMetaData = currentUser.InformationMetadata,
                                TermCode = term.Value.ToString(),
                                Value = value,
                            }).ToArray());
                    }
                }
            }
        }
    }
}