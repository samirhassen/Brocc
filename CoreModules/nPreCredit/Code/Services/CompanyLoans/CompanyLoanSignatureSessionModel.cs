using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanSignatureSessionModel
    {
        public class StaticModel
        {
            public string Version { get; set; }
            public string UnsignedDocumentArchiveKey { get; set; }
            public string ApplicationNr { get; set; }
            public List<SignerModel> Signers { get; set; }
            public string AlternateSignatureSessionId { get; set; }
        }

        public class SignerModel
        {
            public int CustomerId { get; set; }
            public string FirstName { get; set; }
            public DateTime? BirthDate { get; set; }
            public int? SignicatSessionApplicantNr { get; set; }
            public List<string> ListMemberships { get; set; }
        }

        public class StateModel
        {
            public string SignatureSessionId { get; set; }
            public DateTime SignatureSessionExpirationDateUtc { get; set; }
            //Everything here must support merging concurrent update
            public Dictionary<int, DateTime> LatestSentDateByCustomerId { get; set; }
            public Dictionary<int, DateTime> SignedDateByCustomerId { get; set; }
            public string SignedDocumentArchiveKey { get; set; }

            public void MergeLatestSentDate(int customerId, DateTime sentDate)
            {
                LatestSentDateByCustomerId = MergeLatestDate(customerId, sentDate, LatestSentDateByCustomerId);
            }

            public void MergeSignedDate(int customerId, DateTime sentDate)
            {
                SignedDateByCustomerId = MergeLatestDate(customerId, sentDate, SignedDateByCustomerId);
            }

            private Dictionary<int, DateTime> MergeLatestDate(int customerId, DateTime date, Dictionary<int, DateTime> d)
            {
                if (d == null)
                    d = new Dictionary<int, DateTime>();

                if (d.ContainsKey(customerId))
                    d[customerId] = Dates.Max(d[customerId], date);
                else
                    d[customerId] = date;

                return d;
            }

        }
        public StaticModel Static { get; set; }
        public StateModel State { get; set; }

        public bool HasSigned(SignerModel s)
        {
            return State?.SignedDateByCustomerId != null && State.SignedDateByCustomerId.ContainsKey(s.CustomerId);
        }
        public bool HaveAllSignersSigned()
        {
            return Static.Signers.All(HasSigned);
        }
    }
}