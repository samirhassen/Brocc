using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.ElectronicSignatures
{
    public class SingleDocumentSignatureRequest : IValidatableObject
    {
        [Required]
        public string DocumentToSignArchiveKey { get; set; }
        [Required]
        public string DocumentToSignFileName { get; set; }
        [Required]
        public string RedirectAfterFailedUrl { get; set; }
        [Required]
        public string RedirectAfterSuccessUrl { get; set; }
        public string ServerToServerCallbackUrl { get; set; }
        [Required]
        public List<SigningCustomer> SigningCustomers { get; set; }
        public Dictionary<string, string> CustomData { get; set; }

        public class SigningCustomer
        {
            [Required]
            public int? SignerNr { get; set; }
            [Required]
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SigningCustomers != null)
            {
                var count = SigningCustomers.Count();
                if (SigningCustomers.GroupBy(x => x.CivicRegNr).Count() != count)
                    yield return new ValidationResult("Duplicate SigningCustomers.CivicRegNr");

                if (SigningCustomers.GroupBy(x => x.SignerNr).Count() != count)
                    yield return new ValidationResult("Duplicate SigningCustomers.SignerNr");
            }
        }

        //Hack to handle the fact that dotnet standard does not include data annotations
        public static SingleDocumentSignatureRequest CreateFromUnvalidated(SingleDocumentSignatureRequestUnvalidated request)
        {
            if (request == null)
                return null;
            return new SingleDocumentSignatureRequest
            {
                DocumentToSignArchiveKey = request.DocumentToSignArchiveKey,
                RedirectAfterFailedUrl = request.RedirectAfterFailedUrl,
                RedirectAfterSuccessUrl = request.RedirectAfterSuccessUrl,
                CustomData = request.CustomData,
                DocumentToSignFileName = request.DocumentToSignFileName,
                ServerToServerCallbackUrl = request.ServerToServerCallbackUrl,
                SigningCustomers = request.SigningCustomers?.Select(x => x == null ? null : new SingleDocumentSignatureRequest.SigningCustomer
                {
                    CivicRegNr = x.CivicRegNr,
                    CustomData = x.CustomData,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    SignerNr = x.SignerNr
                })?.ToList()
            };
        }

        public SingleDocumentSignatureRequestUnvalidated ToUnvalidated() => new SingleDocumentSignatureRequestUnvalidated
        {
            DocumentToSignArchiveKey = DocumentToSignArchiveKey,
            RedirectAfterFailedUrl = RedirectAfterFailedUrl,
            RedirectAfterSuccessUrl = RedirectAfterSuccessUrl,
            CustomData = CustomData,
            DocumentToSignFileName = DocumentToSignFileName,
            ServerToServerCallbackUrl = ServerToServerCallbackUrl,
            SigningCustomers = SigningCustomers?.Select(x => x == null ? null : new SingleDocumentSignatureRequestUnvalidated.SigningCustomer
            {
                CivicRegNr = x.CivicRegNr,
                CustomData = x.CustomData,
                FirstName = x.FirstName,
                LastName = x.LastName,
                SignerNr = x.SignerNr
            })?.ToList()
        };
    }
}
