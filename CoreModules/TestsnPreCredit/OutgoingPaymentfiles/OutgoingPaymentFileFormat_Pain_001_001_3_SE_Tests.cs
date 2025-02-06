using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TestsnPreCredit.OutgoingPaymentfiles
{
    [TestClass]
    public class OutgoingPaymentFileFormat_Pain_001_001_3_SE_Tests
    {
        [TestMethod]
        public void BalanziaSeHandelsbankenSample1()
        {
            var f = new OutgoingPaymentFileFormat_Pain_001_001_3_SE(true);
            var now = new DateTime(2019, 7, 11, 16, 27, 9);

            var actual = f
                .CreateFile(
                    new OutgoingPaymentFileFormat_Pain_001_001_3_SE.PaymentFile
                    {
                        CurrencyCode = "SEK",
                        ExecutionDate = now,
                        PaymentFileId = "231486",
                        SenderCompanyName = "Balanzia Företagslån",
                        SenderCompanyId = "SenderCompanyId",
                        SendingBankName = "SendingBankName",
                        Groups = new List<OutgoingPaymentFileFormat_Pain_001_001_3_SE.PaymentFile.PaymentGroup>
                        {
                            new OutgoingPaymentFileFormat_Pain_001_001_3_SE.PaymentFile.PaymentGroup
                            {
                                FromAccount = BankAccountNumberSe.Parse("6123472765531"),
                                PaymentGroupId = "231486",
                                Payments = new List<OutgoingPaymentFileFormat_Pain_001_001_3_SE.Payment>
                                {
                                    new OutgoingPaymentFileFormat_Pain_001_001_3_SE.Payment
                                    {
                                        Amount = 97195m,
                                        CustomerName = "Lidingö Import Aktiebolag",
                                        ToAccount = BankAccountNumberSe.Parse("99602607389281"),
                                        Message = "Loan 6526",
                                        PaymentId = "231486"
                                    }
                                }
                            }
                        }
                    },
                    now,
                    OrganisationNumberSe.Parse("5560588740"),
                    "HANDSESS");
            var expected = XDocuments.Parse(KnownCorrectFile);

            var diffs = XmlDiffHelper.GetDiffs(expected, actual);
            if (diffs.Any())
            {
                var failMsg = string.Join(Environment.NewLine, diffs);
                Assert.Fail(failMsg);
                Console.WriteLine(failMsg);
            }
        }

        private const string KnownCorrectFile = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Document xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.001.001.03"">
	<CstmrCdtTrfInitn>
		<GrpHdr>
			<MsgId>231486</MsgId>
			<CreDtTm>2019-07-11T16:27:09</CreDtTm>
			<NbOfTxs>1</NbOfTxs>
			<InitgPty>
				<Nm>Balanzia Företagslån</Nm>
			</InitgPty>
		</GrpHdr>
		<PmtInf>
			<PmtInfId>231486</PmtInfId>
			<PmtMtd>TRF</PmtMtd>
			<PmtTpInf><InstrPrty>NORM</InstrPrty><SvcLvl><Cd>NURG</Cd></SvcLvl><CtgyPurp><Cd>SUPP</Cd></CtgyPurp></PmtTpInf>
			<ReqdExctnDt>2019-07-11</ReqdExctnDt>
			<Dbtr>
				<Nm>Balanzia Företagslån</Nm>
				<PstlAdr><Ctry>SE</Ctry></PstlAdr>
				<Id><OrgId><Othr><Id>5560588740</Id><SchmeNm><Cd>BANK</Cd></SchmeNm></Othr></OrgId></Id>
			</Dbtr>
			<DbtrAcct><Id><Othr><Id>472765531</Id><SchmeNm><Cd>BBAN</Cd></SchmeNm></Othr></Id><Ccy>SEK</Ccy></DbtrAcct>
			<DbtrAgt><FinInstnId><ClrSysMmbId><MmbId>HANDSESS</MmbId></ClrSysMmbId><PstlAdr><Ctry>SE</Ctry></PstlAdr></FinInstnId></DbtrAgt>
			<CdtTrfTxInf>
				<PmtId><EndToEndId>231486</EndToEndId></PmtId>
				<Amt><InstdAmt Ccy=""SEK"">97195</InstdAmt></Amt>
				<CdtrAgt><FinInstnId><ClrSysMmbId><MmbId>9960</MmbId></ClrSysMmbId></FinInstnId></CdtrAgt>
				<Cdtr><Nm>Lidingö Import Aktiebolag</Nm><PstlAdr><Ctry>SE</Ctry></PstlAdr></Cdtr>
				<CdtrAcct><Id><Othr><Id>99602607389281</Id><SchmeNm><Cd>BBAN</Cd></SchmeNm></Othr></Id></CdtrAcct>
				<RmtInf><Ustrd>Loan 6526</Ustrd></RmtInf>
			</CdtTrfTxInf>
		</PmtInf>
	</CstmrCdtTrfInitn>
</Document>
";
    }
}