using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace nSavings.Code.Services.FinnishCustomsAccounts
{
    public class FinnishCustomsMigrationManagerLegacy : IFinnishCustomsMigrationManager
    {
        public static IFinnishCustomsMigrationManager SharedInstance = new FinnishCustomsMigrationManagerLegacy();

        public MemoryStream CreateFlatZipFile(params Tuple<string, Stream>[] fileNamesAndData) => 
            ZipFiles.CreateFlatZipFile(fileNamesAndData);

        public T WithHttpClientUsingClientCertificate<T>(X509Certificate2 clientCertificate, Func<HttpClient, T> withClient)
        {
            using (var h = new WebRequestHandler())
            {
                h.ClientCertificates.Add(clientCertificate);

                var c = new HttpClient(h);
                return withClient(c);
            }
        }
        public void ValidateAndThrowOnError(FinnishCustomsFileFormat.UpdateModel model) =>
            ComponentModelAnnotationsObjectValidator.ValidateAndThrowOnError(model);
    }
}