using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Datasources
{
    public class BankAccountTypeAndNrCreditApplicationItemDataSource : IApplicationDataSource
    {
        private const string DefaultValueInternal = "0bde4e25-ad19-498e-bda1-283b1e8aa570";

        private readonly CreditApplicationItemDataSource standardSource;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public BankAccountTypeAndNrCreditApplicationItemDataSource(ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService,
            IPreCreditContextFactoryService preCreditContextFactoryService, EncryptionService encryptionService)
        {
            this.standardSource = new CreditApplicationItemDataSource(creditApplicationCustomEditableFieldsService, preCreditContextFactoryService, encryptionService);
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public const string DataSourceNameShared = "BankAccountTypeAndNr";

        public string DataSourceName => DataSourceNameShared;

        public bool IsSetDataSupported => true;

        public const string BankAccountTypeAndNrName = DataSourceNameShared;

        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy, Action<string> observeMissingItems = null, Func<string, string> getDefaultValue = null, Action<string> observeChangedItems = null)
        {
            var d = new Dictionary<string, string>();
            if (names.Contains(BankAccountTypeAndNrName))
            {
                var tName = $"application.bankAccountNrType";
                var nrName = $"application.bankAccountNr";
                bool wasChanged = false;
                var r = standardSource.GetItems(applicationNr, new HashSet<string> { tName, nrName },
                    ApplicationDataSourceMissingItemStrategy.UseDefaultValue, getDefaultValue: _ => DefaultValueInternal, observeChangedItems: x =>
                    {
                        if (x.Length > 0)
                            wasChanged = true;
                    });
                var v = $"{r[tName]}#{r[nrName]}";
                if (v == CombineValues(DefaultValueInternal, DefaultValueInternal))
                {
                    if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException)
                        throw new NTechCoreWebserviceException($"Application {applicationNr}: Item '{BankAccountTypeAndNrName}' is missing in the datasource '{DataSourceName}'");
                    else if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.UseDefaultValue)
                        d[BankAccountTypeAndNrName] = getDefaultValue(BankAccountTypeAndNrName);
                }
                else
                {
                    d[BankAccountTypeAndNrName] = v;
                }
                if (wasChanged && observeChangedItems != null)
                {
                    observeChangedItems(BankAccountTypeAndNrName);
                }
            }
            return d;
        }

        public static string CombineValues(string typeValue, string nrValue)
        {
            if (string.IsNullOrWhiteSpace(typeValue) || string.IsNullOrWhiteSpace(nrValue))
                return null;

            return $"{typeValue}#{nrValue}";
        }

        public static Tuple<string, string> SeparateCombinedValues(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Tuple.Create((string)null, (string)null);

            var s = value.Split('#');
            return Tuple.Create(s[0], s[1]);
        }

        public int? SetData(string applicationNr, string compoundItemName, bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue, INTechCurrentUserMetadata currentUser)
        {
            if (compoundItemName != BankAccountTypeAndNrCreditApplicationItemDataSource.BankAccountTypeAndNrName)
                return null;

            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var newValues = SeparateCombinedValues(newValue);
                var currentValues = isMissingCurrentValue ? Tuple.Create((string)null, (string)null) : BankAccountTypeAndNrCreditApplicationItemDataSource.SeparateCombinedValues(currentValue);

                var evt = new Lazy<CreditApplicationEvent>(() => context.CreateAndAddEvent(CreditApplicationEventCode.CreditApplicationItemEdited, applicationNr, null));
                if (newValues.Item1 == currentValues.Item1 && newValues.Item2 == currentValues.Item2)
                    return null;

                var ci1 = CreditApplicationItemDataSource.ChangeCreditApplicationItem(applicationNr, isDelete, newValues.Item1, "application", "bankAccountNrType", isMissingCurrentValue, currentValues.Item1, context, evt, true);
                var ci2 = CreditApplicationItemDataSource.ChangeCreditApplicationItem(applicationNr, isDelete, newValues.Item2, "application", "bankAccountNr", isMissingCurrentValue, currentValues.Item2, context, evt, true);

                if (ci1 != null || ci2 != null)
                    context.SaveChanges();

                return ci2?.Id ?? ci1?.Id;
            }
        }
    }
}