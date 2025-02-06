namespace nDataWarehouse
{

    public class AnalyticsSetting
    {
        public enum SettingCodes
        {
            chosenGraph,
            budgetVsResultStartYear,
            budgetVsResultStartMonth,
            budgets
        }

        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}