using NTech.Banking.IncomingPaymentFiles;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Fileformats
{
    public class IncomingPaymentFileParser
    {
        private Dictionary<string, IIncomingPaymentFileFormat> formats = new Dictionary<string, IIncomingPaymentFileFormat>(StringComparer.InvariantCultureIgnoreCase);

        public IncomingPaymentFileParser(IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings, ILoggingService loggingService)
        {
            var isProduction = envSettings.IsProduction;

            Action<IIncomingPaymentFileFormat> register = n =>
                {
                    if (n.IsSupportedInCountry(clientConfiguration.Country.BaseCountry))
                        formats.Add(n.FileFormatName, n);
                };

            register(new IncomingPaymentFileFormat_BgMax(isProduction, !isProduction)); //In production we allow wierd files since historically bgc has not followed their own file standard
            register(new IncomingPaymentFileFormat_Autogiro(isProduction));
            register(new IncomingPaymentFileFormat_Camt_054_001_02(ex => loggingService.Error(ex, "Could not parse payment file")));
        }

        public bool IsKnownFormat(string formatName)
        {
            return formats.ContainsKey(formatName);
        }

        public List<string> GetKnownFileFormats()
        {
            return formats.Select(x => x.Value.FileFormatName).ToList();
        }

        public bool MightBeAValidFile(byte[] filedata, string fileformat)
        {
            return formats[fileformat].MightBeAValidFile(filedata);
        }

        public bool TryParse(byte[] filedata, string fileformat, out IncomingPaymentFile paymentFile, out string errorMessage)
        {
            return formats[fileformat].TryParse(filedata, out paymentFile, out errorMessage);
        }

        public bool TryParseWithOriginal(byte[] filedata, string filename, string fileformat, out IncomingPaymentFileWithOriginal paymentFile, out string errorMessage)
        {
            var isOk = formats[fileformat].TryParse(filedata, out paymentFile, out errorMessage);
            if (isOk)
            {
                paymentFile.OriginalFileData = filedata;
                paymentFile.OriginalFileName = filename;
            }
            return isOk;
        }
    }
}