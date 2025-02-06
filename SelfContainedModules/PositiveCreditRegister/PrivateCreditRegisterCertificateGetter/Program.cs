using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PrivateCreditRegisterCertificateGetter
{

    //Gets certificate from positive credit register finland
    //And places it into certificateOutputPath
    //https://www.vero.fi/en/positivecreditregister/for-software-developers/
    //https://www.vero.fi/en/positivecreditregister/for-software-developers/stakeholder-testing/instructions-on-the-testing-certificate/
    internal class Program
    {
        private static readonly string keyPairFilePath = $@"C:\temp\pcr_cert\keypairs\keyPair_{DateTime.Now:yyyyMMddHHmmss}.txt"; // Specify the path where you want to save the encryption keypair
        private static readonly string customerId = "xxx"; //customerid that we get from positive credit register
        private static readonly string transferId = "xxx"; //transferid that we get from positive credit register
        private static readonly string transferPassword = "xxx"; //transferpassword that we get from positive credit register
        private static readonly string certificateOutputPath = @"C:\temp\pcr_cert\cert.cer"; // Specify the path where you want to save the certificate

        static void Main(string[] args)
        {
            try
            {
                // Create an instance of the generated proxy client
                var client = new CertificateServicesPortTypeClient();

                var keyPair = GenerateKeyPair();
                var keyPem = new StringBuilder();
                var keyPemWriter = new PemWriter(new StringWriter(keyPem));

                // Log private and public keys into a file 
                LogKeyPair(keyPair);

                keyPemWriter.WriteObject(keyPair.Public);
                keyPemWriter.Writer.Flush();

                var transportKey = RemovePemHeaderFooter(keyPem.ToString());
                var csrData = GenerateCertRequest(keyPair);

                var csr = Convert.FromBase64String(csrData);


                // Create a request object using the generated classes
                var signNewCertificateRequest = new SignNewCertificateRequest
                {
                    Environment = EnvironmentTypes.TEST,
                    CustomerId = customerId,
                    TransferId = transferId,
                    TransferPassword = transferPassword,
                    CertificateRequest = csr
                };


                //Call the signNewCertificate operation
                var response = client.signNewCertificate(signNewCertificateRequest);

                //Process the response
                if (response != null)
                {
                    // Access response properties 
                    var retrievalId = response.RetrievalId;
                    var result = response.Result;
                    var signature = response.Signature;

                    Console.WriteLine($"RetrievalId: {retrievalId}");
                    Console.WriteLine($"Result Status: {result.Status}");
                    Console.WriteLine($"Result xml signature: {signature}");

                    //We have to wait 15 seconds before we retrieve the cert
                    System.Threading.Thread.Sleep(15000);

                    //Now retrieve the cert 
                    var getCertificateRequest = new GetCertificateRequest()
                    {
                        Environment = EnvironmentTypes.TEST,
                        CustomerId = customerId,
                        RetrievalId = retrievalId,
                    };

                    var getCertificateResponse = client.getCertificate(getCertificateRequest);

                    if (getCertificateResponse != null)
                    {
                        var getCertificateResponseResult = getCertificateResponse.Result;
                        var getCertificateResponseCertificate = getCertificateResponse.Certificate;
                        var getCertificateResponseSignature = getCertificateResponse.Signature;

                        Console.WriteLine($"GetCertificate result: {getCertificateResponseResult}");
                        Console.WriteLine($"GetCertificate certificate: {getCertificateResponseCertificate}");
                        Console.WriteLine($"GetCertificate signature: {getCertificateResponseSignature}");

                        using (var stream = File.Create(certificateOutputPath))
                        {
                            stream.Write(getCertificateResponseCertificate, 0, getCertificateResponseCertificate.Length);
                        }

                        Console.WriteLine($"Certificate saved to: {certificateOutputPath}");
                    }

                }
                else
                {
                    Console.WriteLine("No response received from the service.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            // Generate private/public key pair
            RsaKeyPairGenerator generator = new RsaKeyPairGenerator();
            KeyGenerationParameters keyParams = new KeyGenerationParameters(new SecureRandom(), 2048);
            generator.Init(keyParams);
            return generator.GenerateKeyPair();
        }

        private static string RemovePemHeaderFooter(string input)
        {
            var headerFooterList = new List<string>()
    {
                "-----BEGIN CERTIFICATE REQUEST-----",
        "-----END CERTIFICATE REQUEST-----",
        "-----BEGIN PUBLIC KEY-----",
        "-----END PUBLIC KEY-----",
        "-----BEGIN RSA PRIVATE KEY-----",
        "-----END RSA PRIVATE KEY-----"
    };

            string trimmed = input;
            foreach (var hf in headerFooterList)
            {
                trimmed = trimmed.Replace(hf, string.Empty);
            }

            return trimmed.Replace("\r\n", string.Empty);
        }

        private static string GenerateCertRequest(AsymmetricCipherKeyPair keyPair)
        {
            var values = new Dictionary<DerObjectIdentifier, string> {
                {X509Name.CN, "Data Providers Test Issuing CA v1"}, //domain name inside the quotes
                {X509Name.O, "Verohallinto"}, //Organisation\'s Legal name inside the quotes
                {X509Name.C, "FI"},
                };

            var subject = new X509Name(values.Keys.Reverse().ToList(), values);
            var csr = new Pkcs10CertificationRequest(
            new Asn1SignatureFactory("SHA256withRSA", keyPair.Private),
            subject,
            keyPair.Public,
            null,
            keyPair.Private);

            //Convert BouncyCastle csr to PEM format
            var csrPem = new StringBuilder();
            var csrPemWriter = new PemWriter(new StringWriter(csrPem));
            csrPemWriter.WriteObject(csr);
            csrPemWriter.Writer.Flush();
            return RemovePemHeaderFooter(csrPem.ToString());
        }

        private static void LogKeyPair(AsymmetricCipherKeyPair keyPair)
        {
            string privateKeyString = ExportPrivateKey(keyPair.Private);
            string publicKeyString = ExportPublicKey(keyPair.Public);

            var keyPairRawString = ExportKeyPair(keyPair); 
            string keyPairInfo = $"Private Key:{Environment.NewLine}{privateKeyString}{Environment.NewLine}{Environment.NewLine}Public Key:{Environment.NewLine}{publicKeyString}{Environment.NewLine}Raw keypair: {Environment.NewLine}{keyPairRawString}";

            File.WriteAllText(keyPairFilePath, keyPairInfo);
        }

        // Export the private key as a PEM string
        private static string ExportPrivateKey(AsymmetricKeyParameter privateKey)
        {
            if (privateKey is RsaPrivateCrtKeyParameters rsaPrivateKey)
            {
                var privateKeyPem = new StringBuilder();
                var privateKeyPemWriter = new PemWriter(new StringWriter(privateKeyPem));
                privateKeyPemWriter.WriteObject(rsaPrivateKey);
                privateKeyPemWriter.Writer.Flush();
                return privateKeyPem.ToString();
            }
            else
            {
                throw new NotSupportedException("Unsupported private key type");
            }
        }

        // Export the public key as a PEM string
        private static string ExportPublicKey(AsymmetricKeyParameter publicKey)
        {
            if (publicKey is RsaKeyParameters rsaPublicKey)
            {
                var publicKeyPem = new StringBuilder();
                var publicKeyPemWriter = new PemWriter(new StringWriter(publicKeyPem));
                publicKeyPemWriter.WriteObject(rsaPublicKey);
                publicKeyPemWriter.Writer.Flush();
                return publicKeyPem.ToString();
            }
            else
            {
                throw new NotSupportedException("Unsupported public key type");
            }
        }

        private static string ExportKeyPair(AsymmetricCipherKeyPair keyPair)
        {
            var keyPairInfo = new StringBuilder();

            if (keyPair != null)
            {
                // Extract the private key components
                var rsaPrivateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;

                keyPairInfo.AppendLine("Private Key:");
                keyPairInfo.AppendLine($"Modulus: {Convert.ToBase64String(rsaPrivateKey.Modulus.ToByteArray())}");
                keyPairInfo.AppendLine($"Exponent: {rsaPrivateKey.PublicExponent}");
                keyPairInfo.AppendLine($"Private Exponent: {rsaPrivateKey.Exponent}");
                keyPairInfo.AppendLine($"P: {rsaPrivateKey.P}");
                keyPairInfo.AppendLine($"Q: {rsaPrivateKey.Q}");
                keyPairInfo.AppendLine($"DP: {rsaPrivateKey.DP}");
                keyPairInfo.AppendLine($"DQ: {rsaPrivateKey.DQ}");
                keyPairInfo.AppendLine($"InverseQ: {rsaPrivateKey.QInv}");
                keyPairInfo.AppendLine();

                // Extract the public key components
                var rsaPublicKey = (RsaKeyParameters)keyPair.Public;

                keyPairInfo.AppendLine("Public Key:");
                keyPairInfo.AppendLine($"Modulus: {Convert.ToBase64String(rsaPublicKey.Modulus.ToByteArray())}");
                keyPairInfo.AppendLine($"Exponent: {rsaPublicKey.Exponent}");
            }

            return keyPairInfo.ToString();
        }
    }
}


