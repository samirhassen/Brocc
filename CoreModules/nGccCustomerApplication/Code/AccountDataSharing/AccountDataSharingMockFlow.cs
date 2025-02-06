using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

namespace nGccCustomerApplication.Code.AccountDataSharing
{
    public class AccountDataSharingMockFlow
    {
        public static void BeginShare(string token, int applicantNr, TimeSpan callbackDelay)
        {
            var preCreditClient = new PreCreditClientWrapperDirectPart();
            var documentClient = new DocumentClient();
            var state = preCreditClient.GetApplicationState(token);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(callbackDelay);

                var pdf = CreateMinimalPdf($"PSD2 for applicant {applicantNr} at {DateTime.Now}");
                var pdfArchiveKey = documentClient.ArchiveStore(pdf, "application/pdf", $"psd2-{state.ApplicationNr}-{applicantNr}.pdf");
                var rawData = JsonConvert.SerializeObject(new { todo = "account data json from SAT" });
                var rawDataArchiveKey = documentClient.ArchiveStore(Encoding.UTF8.GetBytes(rawData), "application/json", $"psd2-{state.ApplicationNr}-{applicantNr}.json");
                
                preCreditClient.UpdateBankAccountDataShareData(state.ApplicationNr, applicantNr, rawDataArchiveKey, pdfArchiveKey);
            });
        }

        private static byte[] CreateMinimalPdf(string textContent)
        {
            //Based on https://brendanzagaeski.appspot.com/0004.html
            return Encoding.GetEncoding("iso-8859-1").GetBytes(string.Format(@"%PDF-1.1
%¥±ë

1 0 obj
  << /Type /Catalog
     /Pages 2 0 R
  >>
endobj

2 0 obj
  << /Type /Pages
     /Kids [3 0 R]
     /Count 1
     /MediaBox [0 0 300 144]
  >>
endobj

3 0 obj
  <<  /Type /Page
      /Parent 2 0 R
      /Resources
       << /Font
           << /F1
               << /Type /Font
                  /Subtype /Type1
                  /BaseFont /Times-Roman
               >>
           >>
       >>
      /Contents 4 0 R
  >>
endobj

4 0 obj
  << /Length 59 >>
stream
  BT
    /F1 18 Tf
    0 0 Td
    ({0}) Tj
  ET
endstream
endobj

xref
0 5
0000000000 65535 f
0000000021 00000 n
0000000086 00000 n
0000000195 00000 n
0000000490 00000 n
trailer
  <<  /Root 1 0 R
      /Size 5
  >>
startxref
609
%%EOF", textContent));
        }
    }
}