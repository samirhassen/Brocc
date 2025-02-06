namespace nPreCredit.Code.StandardPolicyFilters
{
    /// <summary>
    /// A facade over a variable set that lets you work with only applicant1, applicant2 or application level without providing that on every call.
    /// </summary>
    public class ScopedVariableSet
    {
        private readonly VariableSet variableSet;

        public ScopedVariableSet(VariableSet variableSet, int? forApplicantNr)
        {
            this.variableSet = variableSet;
            ForApplicantNr = forApplicantNr; //This is the scope
        }

        public int? ForApplicantNr { get; set; }

        public string GetString(string name, bool isRequired)
        {
            return ForApplicantNr.HasValue ? variableSet.GetApplicantValue(name, isRequired, ForApplicantNr.Value) : variableSet.GetApplicationValue(name, isRequired);
        }

        public int? GetIntOptional(string name)
        {
            var value = GetString(name, false);
            return value == null ? new int?() : int.Parse(value);
        }

        public int GetIntRequired(string name) => int.Parse(GetString(name, true));
        public decimal GetDecimalRequired(string name) => decimal.Parse(GetString(name, true), System.Globalization.CultureInfo.InvariantCulture);
        public decimal? GetDecimalOptional(string name)
        {
            var value = GetString(name, false);
            return string.IsNullOrWhiteSpace(value) ? new decimal?() : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public bool GetBoolRequired(string name)
        {
            var value = GetString(name, true);
            if (value == "true") return true;
            if (value == "false") return false;
            throw new PolicyFilterException("Invalid boolean variable value. Must be true or false");
        }

        public bool? GetBoolOptional(string name)
        {
            var value = GetString(name, false);
            if (value == null) return new bool?();
            if (value == "true") return true;
            if (value == "false") return false;
            throw new PolicyFilterException("Invalid boolean variable value. Must be true or false");
        }

    }
}