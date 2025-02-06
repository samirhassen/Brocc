using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace nCredit.Code.Sat
{
    public class SatExportFileFormat
    {
        const string FileHeaderPattern = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ns1:batchRecordResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ns1=\"http://www.asiakastieto.fi/XMLSchema/SAT_consumer_loan.xsd\" xsi:schemaLocation=\"http://asiakastieto.fi/XMLSchema/SAT_consumer_loan.xsd\"><ns1:batchRecordResponse><ns1:dateForFetching>{{dateForFetching}}</ns1:dateForFetching><ns1:creditorId>{{creditorId}}</ns1:creditorId><ns1:creditorName>{{creditorName}}</ns1:creditorName>";
        const string FileItemPattern = "<ns1:personRecordRow><ns1:personId>{{civicRegNr}}</ns1:personId><ns1:count>{{countValue}}</ns1:count><ns1:consumerLoan><ns1:consumerLoanRow><ns1:code>c01</ns1:code><ns1:value>{{c01Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>c03</ns1:code><ns1:value>{{c03Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>c04</ns1:code><ns1:value>{{c04Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>h14</ns1:code><ns1:value>{{h14Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>d11</ns1:code><ns1:value>{{d11Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>d12</ns1:code><ns1:value>{{d12Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>e11</ns1:code><ns1:value>{{e11Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>e12</ns1:code><ns1:value>{{e12Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>f11</ns1:code><ns1:value>{{f11Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>f12</ns1:code><ns1:value>{{f12Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>f13</ns1:code><ns1:value>{{f13Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>h15</ns1:code><ns1:value>{{h15Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>h16</ns1:code><ns1:value>{{h16Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>k11</ns1:code><ns1:value>{{k11Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>k12</ns1:code><ns1:value>{{k12Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>t11</ns1:code><ns1:value>{{t11Value}}</ns1:value></ns1:consumerLoanRow><ns1:consumerLoanRow><ns1:code>t12</ns1:code><ns1:value>{{t12Value}}</ns1:value></ns1:consumerLoanRow></ns1:consumerLoan></ns1:personRecordRow>";
        const string FileFooterPattern = "<ns1:endRecord><ns1:numberOfTransactionRecords>{{nrOfCustomers}}</ns1:numberOfTransactionRecords></ns1:endRecord></ns1:batchRecordResponse></ns1:batchRecordResponse>";

        public void WithTemporaryExportFile(Dictionary<int, SatExportItem> items, Action<string> withTempFile)
        {
            var tempFileName = Path.Combine(Path.GetTempPath(), "ntech-satexport-" + Guid.NewGuid() + ".xml");
            try
            {
                var creditorIdAndName = NEnv.SatReportingCreditorIdAndName;
                using (var fs = System.IO.File.CreateText(tempFileName))
                {
                    fs.Write(FileHeaderPattern
                        .Replace("{{dateForFetching}}", DateTime.Today.ToString("ddMMyyyy"))
                        .Replace("{{creditorId}}", creditorIdAndName.Item1)
                        .Replace("{{creditorName}}", creditorIdAndName.Item2));

                    var customerCount = 0;
                    foreach (var c in items)
                    {
                        fs.Write(FileItemPattern
                            .Replace("{{civicRegNr}}", c.Value.CivicRegNr)
                            .Replace("{{countValue}}", c.Value.Count.ToString())
                            .Replace("{{c01Value}}", c.Value.ItemC01.ToString())
                            .Replace("{{c03Value}}", c.Value.ItemC03.ToString())
                            .Replace("{{c04Value}}", c.Value.ItemC04.ToString())
                            .Replace("{{h14Value}}", c.Value.ItemH14.ToString())
                            .Replace("{{d11Value}}", c.Value.ItemD11.ToString())
                            .Replace("{{d12Value}}", c.Value.ItemD12.ToString())
                            .Replace("{{e11Value}}", c.Value.ItemE11.ToString())
                            .Replace("{{e12Value}}", c.Value.ItemE12.ToString())
                            .Replace("{{f11Value}}", c.Value.ItemF11.ToString())
                            .Replace("{{f12Value}}", c.Value.ItemF12.ToString())
                            .Replace("{{f13Value}}", c.Value.ItemF13.ToString())
                            .Replace("{{h15Value}}", c.Value.ItemH15.ToString())
                            .Replace("{{h16Value}}", c.Value.ItemH16.ToString())
                            .Replace("{{k11Value}}", c.Value.ItemK11.HasValue ? c.Value.ItemK11.Value.ToString("yyyy-MM-dd") : "")
                            .Replace("{{k12Value}}", c.Value.ItemK11.HasValue ? c.Value.ItemK12.Value.ToString("yyyy-MM-dd") : "")
                            .Replace("{{t11Value}}", c.Value.ItemT11.HasValue ? c.Value.ItemT11.Value.ToString(CultureInfo.InvariantCulture) : "")
                            .Replace("{{t12Value}}", c.Value.ItemT12.HasValue ? c.Value.ItemT12.Value.ToString("yyyy-MM-dd") : "")
                            );
                        customerCount += 1;
                    }

                    fs.Write(FileFooterPattern.Replace("{{nrOfCustomers}}", customerCount.ToString()));
                }

                withTempFile(tempFileName);
            }
            finally
            {
                if (System.IO.File.Exists(tempFileName))
                {
                    try { System.IO.File.Delete(tempFileName); } catch { /* ignored */ }
                }
            }
        }
    }
}