using Newtonsoft.Json.Linq;
using NTech.Banking.CivicRegNumbers;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCustomer.Services.EidSignatures.Scrive
{
    public class EditableScriveDocument
    {
        private readonly JObject rawDocument;

        public EditableScriveDocument(JObject rawDocument)
        {
            this.rawDocument = rawDocument;
        }

        public JObject RawDocument => this.rawDocument;

        private string GetProperty(string name) => rawDocument.GetStringPropertyValue(name, false);

        public string GetId()
        {
            var id = GetProperty("id");
            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("Invalid document. Missing id");
            return id;
        }

        public void SetTitle(string title)
        {
            rawDocument.AddOrReplaceJsonProperty("title", new JValue(title), false);
        }

        public void SetLanguage(string languageTwoLetterIsoCode)
        {
            rawDocument.AddOrReplaceJsonProperty("lang", new JValue(languageTwoLetterIsoCode), false);
        }

        public void SetApiStatusChangeCallbackUrl(Uri uri)
        {
            RawDocument.AddOrReplaceJsonProperty("api_callback_url", new JValue(uri.ToString()), false);
        }

        public void ChangeAuthorFromSignatoryToViewer()
        {
            foreach (var partyRaw in rawDocument.GetArray("parties", false))
            {
                var party = partyRaw as JObject;
                if (party?.GetBooleanPropertyValue("is_author", false) ?? false)
                {
                    party.AddOrReplaceJsonProperty("is_signatory", new JValue(false), false);
                    party.AddOrReplaceJsonProperty("signatory_role", new JValue("viewer"), false);
                    party.AddOrReplaceJsonProperty("confirmation_delivery_method", new JValue("none"), false);
                    party.AddOrReplaceJsonProperty("delivery_method", new JValue("api"), false);
                }
            }
        }

        public int AddSignatory(ICivicRegNumber civicRegNr, string firstName, string lastName, Uri successRedirectUrl, Uri failedRedirectUrl)
        {
            var parties = rawDocument.GetArray("parties", false);
            var party = new JObject();
            party.AddOrReplaceJsonProperty("is_signatory", new JValue(true), false);
            party.AddOrReplaceJsonProperty("signatory_role", new JValue("signing_party"), false);
            party.AddOrReplaceJsonProperty("confirmation_delivery_method", new JValue("none"), false);
            party.AddOrReplaceJsonProperty("delivery_method", new JValue("api"), false);

            var fields = new JArray();
            party.Add("fields", fields);

            void AddName(string value, int order)
            {
                var nameField = new JObject();
                nameField.AddOrReplaceJsonProperty("type", new JValue("name"), false);
                nameField.AddOrReplaceJsonProperty("order", new JValue(order), false);
                nameField.AddOrReplaceJsonProperty("value", new JValue(value), false);
                nameField.AddOrReplaceJsonProperty("is_obligatory", new JValue(true), false);
                nameField.AddOrReplaceJsonProperty("should_be_filled_by_sender", new JValue(false), false);
                fields.Add(nameField);
            }

            AddName(firstName, 1);
            AddName(lastName, 2);

            if (civicRegNr.Country == "SE")
            {
                var civicNrField = new JObject();
                civicNrField.AddOrReplaceJsonProperty("type", new JValue("personal_number"), false);
                civicNrField.AddOrReplaceJsonProperty("value", new JValue(civicRegNr.NormalizedValue), false);
                civicNrField.AddOrReplaceJsonProperty("is_obligatory", new JValue(true), false);
                civicNrField.AddOrReplaceJsonProperty("should_be_filled_by_sender", new JValue(true), false);
                fields.Add(civicNrField);
                party.AddOrReplaceJsonProperty("authentication_method_to_view", new JValue("se_bankid"), false);
                party.AddOrReplaceJsonProperty("authentication_method_to_view_archived", new JValue("se_bankid"), false);
                party.AddOrReplaceJsonProperty("authentication_method_to_sign", new JValue("se_bankid"), false);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (successRedirectUrl != null)
            {
                party.AddOrReplaceJsonProperty("sign_success_redirect_url", new JValue(successRedirectUrl.ToString()), false);
            }

            if (failedRedirectUrl != null)
            {
                party.AddOrReplaceJsonProperty("reject_redirect_url", new JValue(failedRedirectUrl.ToString()), false);
            }

            parties.Add(party);

            return parties.Count - 1;
        }

        /// <summary>
        /// 
        /// preparation
        /// pending
        /// closed: this means everyone has signed an no further changes are possible
        /// canceled
        /// timedout
        /// rejected
        /// document_error
        /// </summary>
        public string GetStatus() => rawDocument.GetStringPropertyValue("status", false);

        public bool IsActive() => GetStatus().IsOneOf("preparation", "pending");
        public bool IsSignedByAll() => GetStatus() == "closed";

        /// <summary>
        /// Index is index in the array parties. 
        /// The ones returned are all parties with is_signatory = true who have an api_delivery_url and no sign_time 
        /// </summary>
        public PartySignatureStatus GetSignatureStatusForLocalCustomer(Uri baseUri, CommonElectronicIdSignatureSession.SigningCustomer customer)
        {
            var result = new Dictionary<int, PartySignatureStatus>();

            var scriveSignerIndex = customer.GetCustomDataOpt("scriveSignerIndex");

            var parties = rawDocument.GetArray("parties", false);
            var partyRaw = parties[int.Parse(scriveSignerIndex)];

            var party = partyRaw as JObject;
            if (party?.GetBooleanPropertyValue("is_signatory", false) ?? false)
            {
                //Typical look: "api_delivery_url": "/s/8222115557376659733/3276942/a1f2ea9330969d42"
                var url = party.GetStringPropertyValue("api_delivery_url", false);
                var signtime = party.GetStringPropertyValue("sign_time", false);
                return new PartySignatureStatus
                {
                    SignatureUrl = string.IsNullOrWhiteSpace(url) ? null : NTechServiceRegistry.CreateUrl(baseUri, url),
                    HasSigned = !string.IsNullOrWhiteSpace(signtime)
                };
            }
            else
                throw new Exception("Not a signatory");
        }

        public class PartySignatureStatus
        {
            public Uri SignatureUrl { get; set; }
            public bool HasSigned { get; set; }
        }
    }
}