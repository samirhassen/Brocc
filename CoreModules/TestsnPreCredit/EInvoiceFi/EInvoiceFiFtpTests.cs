using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code.EInvoiceFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static nCredit.Code.EInvoiceFi.EInvoiceFiSftpMessageFileImporter;

namespace TestsnPreCredit.EInvoiceFi
{
    [TestClass]
    public class EInvoiceFiFtpTests
    {
        private class MockFtpSource : IFtpSource
        {
            private Dictionary<string, List<IFtpItem>> filesByDirectoryPath = new Dictionary<string, List<IFtpItem>>();

            public List<IFtpItem> ListDirectory(string directoryPath)
            {
                if (!filesByDirectoryPath.ContainsKey(directoryPath))
                    filesByDirectoryPath[directoryPath] = new List<IFtpItem>();
                return filesByDirectoryPath[directoryPath];
            }

            public MockFtpItem AddTestItem(string directoryPath, string filename, TimeSpan? age = null, byte[] fileData = null, bool isDirectory = false)
            {
                if (!filesByDirectoryPath.ContainsKey(directoryPath))
                    filesByDirectoryPath[directoryPath] = new List<IFtpItem>();

                var defaultContent = Encoding.UTF8.GetBytes("testcontent");

                var fd = fileData ?? (isDirectory ? null : defaultContent);
                var i = new MockFtpItem
                {
                    FileData = fd,
                    IsDirectory = isDirectory,
                    FilenameWithoutPath = filename,
                    LastWriteTimeUtc = DateTime.UtcNow.Subtract(age ?? TimeSpan.FromHours(3)),
                    LengthInBytes = fd?.Length ?? 0
                };
                filesByDirectoryPath[directoryPath].Add(i);
                return i;
            }
        }

        public class MockFtpItem : IFtpItem
        {
            public string FilenameWithoutPath { get; set; }

            public DateTime LastWriteTimeUtc { get; set; }

            public long LengthInBytes { get; set; }

            public bool IsDirectory { get; set; }

            public void Delete()
            {
                if (FileData == null)
                    throw new Exception();
                DeleteCallCount += 1;
            }

            public void Download(Stream target)
            {
                if (FileData == null)
                    throw new Exception();
                target.Write(FileData, 0, FileData.Length);
                DownloadCallCount += 1;
            }

            public byte[] FileData { get; set; }
            public int DownloadCallCount { get; set; }
            public int DeleteCallCount { get; set; }
        }

        [TestMethod]
        public void Download_EndToEndScenario()
        {
            var source = new MockFtpSource();
            var i = EInvoiceFiSftpMessageFileImporter.CreateForTesting(source, 1);

            var dir1 = source.AddTestItem("/test", ".", isDirectory: true);
            var dir2 = source.AddTestItem("/test", "..", isDirectory: true);
            var standardFile = source.AddTestItem("/test", "file_standard.xml");
            var wrongFolderFile = source.AddTestItem("/test2", "file_wrongfolder.xml");
            var recentlyChangedFile = source.AddTestItem("/test", "file_recentlychanged.xml", TimeSpan.FromMilliseconds(10));

            bool importCalled = false;
            i.ImportAndRemoveFiles("/test", null, (stream, filename) =>
            {
                importCalled = true;
                Assert.AreEqual("file_standard.xml", filename);
                return true;
            });

            Assert.IsTrue(importCalled);

            Assert.AreEqual(0, dir1.DownloadCallCount);
            Assert.AreEqual(0, dir1.DeleteCallCount);
            Assert.AreEqual(0, dir2.DownloadCallCount);
            Assert.AreEqual(0, dir2.DeleteCallCount);
            Assert.AreEqual(0, wrongFolderFile.DownloadCallCount);
            Assert.AreEqual(0, wrongFolderFile.DeleteCallCount);
            Assert.AreEqual(0, recentlyChangedFile.DownloadCallCount);
            Assert.AreEqual(0, recentlyChangedFile.DeleteCallCount);

            Assert.AreEqual(1, standardFile.DownloadCallCount);
            Assert.AreEqual(1, standardFile.DeleteCallCount);
        }

        [TestMethod]
        public void Download_RegexTest()
        {
            var source = new MockFtpSource();
            var i = EInvoiceFiSftpMessageFileImporter.CreateForTesting(source, 1);

            var standardFile = source.AddTestItem("/test", "file_standard.xml");
            var wrongExtensionFile = source.AddTestItem("/test", "file_standard.xlsx");
            var wrongPrefixFile = source.AddTestItem("/test", "other_file.xml");

            bool importCalled = false;
            i.ImportAndRemoveFiles("/test", new System.Text.RegularExpressions.Regex(@"file_standard(.*).xml"), (stream, filename) =>
            {
                importCalled = true;
                Assert.AreEqual("file_standard.xml", filename);
                return true;
            });

            Assert.IsTrue(importCalled);
        }

        private const string ExampleFile =
@"<EInvoiceMessages>
    <EInvoiceMessage>
        <MessageId>34rff12</MessageId>
        <MessageType>start</MessageType>
        <Timestamp>20180219235800</Timestamp>
        <CustomerName>åäö</CustomerName>
        <CustomerAddressStreet>Street 1</CustomerAddressStreet>
        <CustomerAddressZipcode>12345</CustomerAddressZipcode>
        <CustomerAddressArea>Helsinki</CustomerAddressArea>
        <CustomerLanguageCode>FI</CustomerLanguageCode>
        <LastInvoicePaidOcr>1235435345</LastInvoicePaidOcr>
        <CustomerIdentification1>test@example.org</CustomerIdentification1>
        <CustomerIdentification2>111119725</CustomerIdentification2>
        <EInvoiceAddress>adr1</EInvoiceAddress>
        <EInvoiceBankCode>bank1</EInvoiceBankCode>
    </EInvoiceMessage>
</EInvoiceMessages>";

        [TestMethod]
        public void TestImportFile()
        {
            var fileData = new MemoryStream(Encoding.UTF8.GetBytes(ExampleFile));

            var parser = new EInvoiceFiIncomingMessageFileFormat();
            string failedMessage;
            IList<EInvoiceFiIncomingMessageFileFormat.Message> messages;
            Assert.IsTrue(parser.TryParseFile(fileData, out messages, out failedMessage), failedMessage);
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("åäö", messages.Single().CustomerName);
        }
    }
}
