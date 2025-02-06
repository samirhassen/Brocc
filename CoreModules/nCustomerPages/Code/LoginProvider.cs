using nCustomerPages.Code;
using NTech.Banking.CivicRegNumbers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace nCustomerPages
{
    public class LoginProvider
    {
        public const string CreditCustomerRoleName = "CreditCustomer";
        public const string SavingsCustomerRoleName = "SavingsCustomer";
        public const string MortageLoanApplicationRoleName = "MortageLoanApplicationCustomer";
        public const string SavingsOrCreditCustomerRoleName = "SavingsCustomer,CreditCustomer";
        public const string EmbeddedMortageLoanCustomerPagesCustomer = "EmbeddedMortageLoanCustomerPagesCustomer";
        public const string EmbeddedCustomerPagesStandardRoleName = "EmbeddedCustomerPagesStandardCustomer";

        private const string RoleClaimName = "ntech.claims.role";
        public const string CustomerIdClaimName = "ntech.claims.customerid";

        public void SignIn(Microsoft.Owin.IOwinContext context, int customerId, string firstName, bool isStrongIdentity, string authType, ICivicRegNumber civicRegNr, (string Name, string CustomData)? reloginTarget)
        {
            var claims = new List<Claim>();

            claims.Add(new Claim("ntech.claims.name", firstName ?? $"Customer{customerId}"));
            claims.Add(new Claim("ntech.claims.hasname", string.IsNullOrWhiteSpace(firstName) ? "false" : "true"));
            claims.Add(new Claim(CustomerIdClaimName, customerId.ToString()));
            claims.Add(new Claim("ntech.claims.isstrongidentity", isStrongIdentity ? "true" : "false"));
            if (civicRegNr != null)
                claims.Add(new Claim("ntech.claims.civicregnr", civicRegNr.NormalizedValue));
            if (reloginTarget.HasValue)
            {
                claims.Add(new Claim("ntech.claims.relogintargetname", reloginTarget.Value.Name));
                claims.Add(new Claim("ntech.claims.relogintargetcustomdata", reloginTarget.Value.CustomData));
            }

            AppendRoleClaims(customerId, claims.Add);

            var identity = new ClaimsIdentity(claims, authType, "ntech.claims.name", "ntech.claims.role");

            context.Authentication.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = false, RedirectUri = null }, identity);

            LogUserAction("LoggedIn", identity);
        }

        private void AppendRoleClaims(int customerId, Action<Claim> appendRoleClaim)
        {
            if (!NEnv.IsMortgageLoansEnabled)
            {
                if (NEnv.IsCreditOverviewActive && !NEnv.IsStandardUnsecuredLoansEnabled)
                {
                    var c = new CustomerLockedCreditClient(customerId);
                    if (c.HasOrHasEverHadACredit())
                        appendRoleClaim(new Claim("ntech.claims.role", CreditCustomerRoleName));
                }
            }
            else if (!NEnv.IsStandardMortgageLoansEnabled)
            {
                if (NEnv.IsEmbeddedMortageLoanCustomerPagesEnabled)
                {
                    var c = new CustomerLockedCreditClient(customerId);
                    if (c.HasOrHasEverHadACredit())
                        appendRoleClaim(new Claim("ntech.claims.role", EmbeddedMortageLoanCustomerPagesCustomer));
                }
            }

            if (NEnv.IsSavingsOverviewActive)
            {
                var c = new SystemUserSavingsClient();
                if (c.HasOrHasEverHadASavingsAccount(customerId))
                    appendRoleClaim(new Claim("ntech.claims.role", SavingsCustomerRoleName));
            }

            if (NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled || NEnv.IsCustomerPagesKycQuestionsEnabled)
            {
                appendRoleClaim(new Claim("ntech.claims.role", EmbeddedCustomerPagesStandardRoleName));
            }
        }

        public static int? GetCustomerId(ClaimsIdentity user)
        {
            var v = user?.FindFirst(CustomerIdClaimName)?.Value;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            int i;
            if (int.TryParse(v, out i))
                return i;
            else
                return null;
        }

        public void ReloginUserToPickUpRoleChanges(Microsoft.Owin.IOwinContext context)
        {
            var a = context?.Authentication?.User?.Identity as ClaimsIdentity;
            var customerId = GetCustomerId(a);
            if (!customerId.HasValue)
                return;

            foreach (var c in a.FindAll(RoleClaimName).ToList())
                a.RemoveClaim(c);

            AppendRoleClaims(customerId.Value, a.AddClaim);

            context?.Authentication?.SignOut();
            context?.Authentication?.SignIn(a);
        }

        public void SignOut(Microsoft.Owin.IOwinContext context)
        {
            LogUserAction("LoggedOut", context?.Authentication?.User?.Identity as ClaimsIdentity);
            context?.Authentication?.SignOut();
        }

        public static void LogUserAction(string actionName, ClaimsIdentity u)
        {
            var customerId = u?.FindFirst("ntech.claims.customerid")?.Value;
            var isStrongIdentity = u?.FindFirst("ntech.claims.isstrongidentity")?.Value;
            var isSystemUser = u?.FindFirst("ntech.issystemuser")?.Value;
            var authenticationType = u?.AuthenticationType;

            var userDesc = $"(customerId={customerId},isStrongIdentity={isStrongIdentity},isSystemUser={isSystemUser},authType={authenticationType})";
            NLog.Information(
                "CustomerPages({EventType}): User {CustomerPagesUser}",
                "CustomerPagesUserAction_" + actionName,
                userDesc);
        }

        public static List<string> GetAuthenticatedUserRoles(System.Security.Principal.IPrincipal user)
        {
            var roles = new List<string>();
            var claimsIdentity = user?.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
                return roles;

            if (!claimsIdentity.IsAuthenticated)
                return roles;

            roles.AddRange(claimsIdentity
                .Claims
                .Where(x => x.Type == "ntech.claims.role")
                .Select(x => x.Value));

            return roles;
        }
    }
}