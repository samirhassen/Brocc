using Newtonsoft.Json;
using NTech.Core.PreCredit.Shared;
using System;
using System.Linq;

namespace nPreCredit.Code
{
    public static class AgreementSigningProviderHelper
    {
        public static CreditApplicationOneTimeToken GetOrCreateApplicationWrapperToken(IPreCreditContextExtended context, DateTimeOffset now, string applicationNr, int applicantNr, int currentUserId, string informationMetadata)
        {
            var tokenName = $"ApplicationWrapperToken{applicantNr}";
            var token = context.CreditApplicationOneTimeTokensQueryable.OrderByDescending(x => x.Timestamp).FirstOrDefault(x => x.ApplicationNr == applicationNr && x.TokenType == tokenName && !x.RemovedBy.HasValue && x.ValidUntilDate >= now);
            if (token != null)
                return token;

            token = new CreditApplicationOneTimeToken
            {
                ApplicationNr = applicationNr,
                CreationDate = now,
                ChangedDate = now,
                ChangedById = currentUserId,
                InformationMetaData = informationMetadata,
                Token = CreditApplicationOneTimeToken.GenerateUniqueToken(),
                TokenType = tokenName,
                ValidUntilDate = now.AddMonths(6),
                TokenExtraData = JsonConvert.SerializeObject(new { applicantNr = applicantNr })
            };

            context.AddCreditApplicationOneTimeTokens(token);

            return token;
        }

    }
}