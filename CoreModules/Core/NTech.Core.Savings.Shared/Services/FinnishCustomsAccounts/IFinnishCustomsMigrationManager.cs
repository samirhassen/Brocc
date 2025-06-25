using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts
{
    /// <summary>
    /// Things that differ between core and legacy where we want to delay migration
    /// - WebRequestHandler to HttpClientHandler migration: difficult to test and initial usecase is just integration tests locally
    /// - Zip files: Not needed for integration tests
    /// - Model validation: Difficult to manage for prod
    /// </summary>
    public interface IFinnishCustomsMigrationManager
    {
        T WithHttpClientUsingClientCertificate<T>(X509Certificate2 clientCertificate, Func<HttpClient, T> withClient);
        MemoryStream CreateFlatZipFile(params Tuple<string, Stream>[] fileNamesAndData);
        void ValidateAndThrowOnError(FinnishCustomsFileFormat.UpdateModel model);
    }
}