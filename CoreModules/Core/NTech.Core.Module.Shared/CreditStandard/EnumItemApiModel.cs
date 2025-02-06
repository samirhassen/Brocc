namespace NTech.Services.Infrastructure.CreditStandard
{
    /// <summary>
    /// Exposed in webservice apis
    /// </summary>
    public class EnumItemApiModel
    {
        public EnumItemApiModel() { }

        public EnumItemApiModel(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; set; }
        public string DisplayName { get; set; }
    }
}
