namespace nCustomerPages
{
    public class TitleAndBackModel
    {
        /// <summary>
        /// Title comes translation
        /// </summary>
        public static TitleAndBackModel Translated(string titleKey, string backUrl)
        {
            return new TitleAndBackModel
            {
                TitleSource = titleKey,
                BackUrlSource = backUrl,
                IsTitleSourceTranslationKey = true
            };
        }

        /// <summary>
        /// Title come from an expression like credit.creditNr or $ctrl.getTitle()
        /// </summary>
        public static TitleAndBackModel Dynamic(string titleSource, string backUrl)
        {
            return new TitleAndBackModel
            {
                TitleSource = titleSource,
                BackUrlSource = backUrl,
                IsTitleSourceTranslationKey = false
            };
        }

        /// <summary>
        /// Angular source of back (something like title or $ctrl.title
        /// </summary>
        public string TitleSource { get; set; }

        public bool IsTitleSourceTranslationKey { get; set; }

        /// <summary>
        /// Angular source of back (something like backUrl or $ctrl.backUrl
        /// </summary>
        public string BackUrlSource { get; set; }
    }
}