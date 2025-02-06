namespace nPreCredit.Code.Balanzia
{
    public class BalanziaCampaignCode : ICampaignCode
    {
        private readonly bool disableRemoveInitialFee;
        private readonly bool disableForceManualControl;

        public BalanziaCampaignCode(bool disableRemoveInitialFee, bool disableForceManualControl)
        {
            this.disableRemoveInitialFee = disableRemoveInitialFee;
            this.disableForceManualControl = disableForceManualControl;
        }

        public static BalanziaCampaignCode CreateWithAllRulesActive()
        {
            return new BalanziaCampaignCode(false, false);
        }

        private static string NormalizeString(string input)
        {
            return (input ?? "").Trim().ToUpperInvariant();
        }

        public bool IsCodeThatForcesManualControl(string code)
        {
            if (disableForceManualControl)
                return false;
            var n = NormalizeString(code);
            return IsValidCode(n) && n[5] == '9';
        }

        public bool IsCodeThatRemovesInitialFee(string code)
        {
            if (disableRemoveInitialFee)
                return false;
            var n = NormalizeString(code);
            return IsValidCode(n) && n[5] == '1';
        }

        public bool IsValidCode(string code)
        {
            var n = NormalizeString(code);
            return code.Length >= 6 && char.IsLetter(n[0]);
        }
    }

    public class DoNothingCampaignCode : ICampaignCode
    {
        public bool IsCodeThatForcesManualControl(string code)
        {
            return false;
        }

        public bool IsCodeThatRemovesInitialFee(string code)
        {
            return false;
        }

        public bool IsValidCode(string code)
        {
            return true;
        }
    }

    public interface ICampaignCode
    {
        bool IsValidCode(string code);

        bool IsCodeThatForcesManualControl(string code);
        bool IsCodeThatRemovesInitialFee(string code);
    }
}