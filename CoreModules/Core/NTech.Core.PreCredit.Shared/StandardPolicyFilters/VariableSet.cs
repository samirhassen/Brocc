using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class VariableSet
    {
        public VariableSet(int nrOfApplicants)
        {
            this.NrOfApplicants = nrOfApplicants;
        }
        public Dictionary<string, string> ApplicationValues { get; set; }
        public Dictionary<int, Dictionary<string, string>> ApplicantValues { get; set; }
        public int NrOfApplicants { get; set; }

        public List<int> GetApplicantNrs()
        {
            if (NrOfApplicants == 0)
                throw new Exception("NrOfApplicants not set");
            return Enumerable.Range(1, NrOfApplicants).ToList();
        }

        public string GetApplicationValue(string name, bool isRequired)
        {
            var value = ApplicationValues.Opt(name);
            if (isRequired && value == null)
                throw new PolicyFilterException($"Missing application level variable {name}")
                {
                    IsMissingApplicationLevelVariable = true,
                    MissingVariableOrParameterName = name
                };

            return value;
        }

        public string GetApplicantValue(string name, bool isRequired, int applicantNr)
        {
            var value = ApplicantValues.Opt(applicantNr)?.Opt(name);
            if (isRequired && value == null)
                throw new PolicyFilterException($"Missing applicant level variable {name} for at least applicant: {applicantNr}")
                {
                    IsMissingApplicantLevelVariable = true,
                    MissingVariableOrParameterName = name,
                    MissingApplicantLevelApplicantNrs = new[] { applicantNr }.ToHashSetShared()
                };

            return value;
        }

        public VariableSet SetApplicantValue(string name, int applicantNr, string value)
        {
            if (ApplicantValues == null)
                ApplicantValues = new Dictionary<int, Dictionary<string, string>>();
            if (!ApplicantValues.ContainsKey(applicantNr))
                ApplicantValues[applicantNr] = new Dictionary<string, string>();
            ApplicantValues[applicantNr][name] = value;
            return this;
        }

        public VariableSet SetApplicantValue(string name, int applicantNr, int value) =>
            SetApplicantValue(name, applicantNr, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public VariableSet SetApplicantValue(string name, int applicantNr, decimal value) =>
            SetApplicantValue(name, applicantNr, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public VariableSet SetApplicantValue(string name, int applicantNr, bool value) =>
            SetApplicantValue(name, applicantNr, value ? "true" : "false");

        public VariableSet SetApplicationValue(string name, string value)
        {
            if (ApplicationValues == null)
                ApplicationValues = new Dictionary<string, string>();
            ApplicationValues[name] = value;
            return this;
        }

        public VariableSet SetApplicationValue(string name, int value) =>
            SetApplicationValue(name, value.ToString());

        public VariableSet SetApplicationValue(string name, decimal value) =>
            SetApplicationValue(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public VariableSet SetApplicationValue(string name, bool value) =>
            SetApplicationValue(name, value ? "true" : "false");

        public VariableSet SetValues(VariableSet source)
        {
            if (source == null)
                return this;

            if (NrOfApplicants != source.NrOfApplicants)
                throw new Exception("Not the same nr of applicants");

            foreach (var applicantNr in source.GetApplicantNrs())
            {
                var applicantValues = source.ApplicantValues.Opt(applicantNr);
                if (applicantValues != null)
                {
                    foreach (var nameAndValue in applicantValues)
                        SetApplicantValue(nameAndValue.Key, applicantNr, nameAndValue.Value);
                }
            }
            if (source.ApplicationValues != null)
            {
                foreach (var nameAndValue in source.ApplicationValues)
                    SetApplicationValue(nameAndValue.Key, nameAndValue.Value);
            }
            return this;
        }
    }
}