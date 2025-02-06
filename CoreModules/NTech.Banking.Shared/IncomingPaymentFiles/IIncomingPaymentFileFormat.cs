using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.IncomingPaymentFiles
{
    public interface IIncomingPaymentFileFormat
    {
        string FileFormatName { get; }
        bool TryParse<TIncomingPaymentFile>(byte[] filedata, out TIncomingPaymentFile paymentFile, out string errorMessage) where TIncomingPaymentFile : IncomingPaymentFile, new();
        bool MightBeAValidFile(byte[] filedata);
        bool IsSupportedInCountry(string countryIsoCode);
    }
}