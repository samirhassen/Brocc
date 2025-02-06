using System.Text;

namespace NTech.Core.TestSupport
{
    public static class TestPdfs
    {
        private static string GenerateMinimalPdfContent(string text) => $"%PDF-1.2\r\n9 0 obj\r\n<<\r\n>>\r\nstream\r\nBT/ 9 Tf({text})' ET\r\nendstream\r\nendobj\r\n4 0 obj\r\n<<\r\n/Type /Page\r\n/Parent 5 0 R\r\n/Contents 9 0 R\r\n>>\r\nendobj\r\n5 0 obj\r\n<<\r\n/Kids [4 0 R ]\r\n/Count 1\r\n/Type /Pages\r\n/MediaBox [ 0 0 300 50 ]\r\n>>\r\nendobj\r\n3 0 obj\r\n<<\r\n/Pages 5 0 R\r\n/Type /Catalog\r\n>>\r\nendobj\r\ntrailer\r\n<<\r\n/Root 3 0 R\r\n>>\r\n%%EOF";
        public static byte[] GetMinimalPdfBytes(string text) => Encoding.UTF8.GetBytes(GenerateMinimalPdfContent(text));
        /*
generateTestPdfDataUrl(text: string): string {
            //Small pdf from https://stackoverflow.com/questions/17279712/what-is-the-smallest-possible-valid-pdf
            let pdfData = `%PDF-1.2
9 0 obj
<<
>>
stream
BT/ 9 Tf(${text})' ET
endstream
endobj
4 0 obj
<<
/Type /Page
/Parent 5 0 R
/Contents 9 0 R
>>
endobj
5 0 obj
<<
/Kids [4 0 R ]
/Count 1
/Type /Pages
/MediaBox [ 0 0 300 50 ]
>>
endobj
3 0 obj
<<
/Pages 5 0 R
/Type /Catalog
>>
endobj
trailer
<<
/Root 3 0 R
>>
%%EOF`

            return 'data:application/pdf;base64,' + btoa(unescape(encodeURIComponent(pdfData)))
        }
    }         
         */
    }
}
