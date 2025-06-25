using System;
using System.Globalization;
using System.IO;
using NTech.Services.Infrastructure;

namespace nSavings.Code.Treasury;

public class TreasuryFileFormat
{
    private string CreateFile(TreasuryDomainModel model, DateTime deliveryDate)
    {
        var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
        const string sep = ";";
        using var fs = File.CreateText(tempFileName);
        var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile",
            "Treasury-business-credit-settings.txt", true);

        var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

        if (string.IsNullOrWhiteSpace(f.Opt("ProductTypeSavingsAccount")))
            throw new InvalidOperationException("ProductTypeSavingsAccount is missing in " +
                                                file.FullName);
        if (string.IsNullOrWhiteSpace(f.Opt("PrefixSavingsCustomerId")))
            throw new InvalidOperationException("PrefixSavingsCustomerId is missing in " +
                                                file.FullName);
        if (string.IsNullOrWhiteSpace(f.Opt("PrefixSavingsAccountNo")))
            throw new InvalidOperationException("PrefixSavingsAccountNo is missing in " + file.FullName);

        fs.WriteLine("Reporting Date" + sep + "Product type" + sep + "General ledger account" + sep +
                     "Customer Id" + sep + "Customer full name" + sep + "Customer country" + sep +
                     "Account number" + sep + "Current balance" + sep + "Currency" + sep +
                     "Account creation date" + sep + "Current interest rate");

        foreach (var t in model.Accounts)
        {
            fs.WriteLine(deliveryDate.ToString("yyyy-MM-dd") + sep +
                         f.Req("ProductTypeSavingsAccount") + sep +
                         f.Req("GeneralLedgerSavingsccount") + sep +
                         f.Req("PrefixSavingsCustomerId") + t.CustomerId + sep +
                         t.CustomerFullName + sep +
                         t.CustomerCountry + sep +
                         f.Req("PrefixSavingsAccountNo") + t.SavingsAccountNr + sep +
                         Math.Round(t.CurrentBalance, 4).ToString(CultureInfo.InvariantCulture) + sep +
                         NEnv.ClientCfg.Country.BaseCurrency + sep +
                         t.StartDate.ToString("yyyy-MM-dd") + sep +
                         Math.Round(t.CurrentInterestRate, 4).ToString(CultureInfo.InvariantCulture));
        }

        return tempFileName;
    }

    public void WithTemporaryExportFile(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
    {
        var tmp = CreateFile(model, deliveryDate);
        try
        {
            withFile(tmp);
        }
        finally
        {
            try
            {
                File.Delete(tmp);
            }
            catch
            {
                /*ignored*/
            }
        }
    }
}