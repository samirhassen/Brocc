namespace nPreCredit
{
    ///<summary>
    /// Not using infrastructure-item but instead opting for using HComplexApplicationListItem
    /// Experience from ComplexApplicationListItem is that the size of that table matter alot for performance
    /// so this is better since all the writes go through the same abstraction anyway.
    ///
    /// The abstraction here is that there is a list that is used in a part of a workflow (like a specific list in a ui)
    ///
    /// For each of these there are several rows, represented by Nr:s.
    /// Each row has a dictionary of single item values and a dictionary of lists.
    ///
    /// Example, two collaterals used on a mortgage loan application with connected customers:
    ///
    /// Row
    /// {
    ///    ApplicationNr = A1
    ///    ListName = MortageObjectCollateral
    ///    Nr = 1
    ///    UniqueItems = { "isObject" : false, "priceAmount" : "100000" }
    ///    RepeatedItems = { "customerIds: ["42", "43"] }
    /// }
    /// Row
    /// {
    ///    ApplicationNr = A1
    ///    ListName = MortageObjectCollateral
    ///    Nr = 2
    ///    UniqueItems = { "isObject" : true, "priceAmount" : "150000" }
    ///    RepeatedItems = { "customerIds: ["42"] }
    /// }
    ///
    /// This would be represented as (applicationNr, namespace, nr, itemName, itemValue, isRepeatable):
    /// (A1, MortageObjectCollateral, 1, isObject, false)
    /// (A1, MortageObjectCollateral, 1, priceAmount, 100000, false)
    /// (A1, MortageObjectCollateral, 1, customerIds, 42, true)
    /// (A1, MortageObjectCollateral, 1, customerIds, 43, true)
    ///
    /// (A1, MortageObjectCollateral, 2, isObject, true)
    /// (A1 MortageObjectCollateral, 2, priceAmount, 150000, false)
    /// (A1, MortageObjectCollateral, 2, customerIds, 42, true)
    ///
    ///</summary>
    public class ComplexApplicationListItem : ComplexApplicationListItemBase
    {
        public int Id { get; set; } //Never used for logic but having autoincrement key is practical sometimes
        public string ApplicationNr { get; set; }
        public CreditApplicationHeader Application { get; set; }
        public CreditApplicationEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public CreditApplicationEvent LatestChangeEvent { get; set; }
        public int LatestChangeEventId { get; set; }
    }

    public class ComplexApplicationListItemBase
    {
        public string ListName { get; set; }
        public int Nr { get; set; }
        public string ItemName { get; set; }

        /// <summary>
        /// NOTE: Set this to true for all ItemsNames where there could be multiple items even if there is only one in this particular case
        /// </summary>
        public bool IsRepeatable { get; set; }
        public string ItemValue { get; set; }
    }

    /// <summary>
    /// All changes are written here also. Intentionally not FK:d to the application so it can be deleted without this following.
    /// </summary>
    public class HComplexApplicationListItem
    {
        public int Id { get; set; } //NOTE: Autoincrement key in the history table. Not the same as ApplicationCollateralItem.Id
        public string ApplicationNr { get; set; }
        public string ListName { get; set; }
        public int Nr { get; set; }
        public string ItemName { get; set; }
        public bool IsRepeatable { get; set; }
        public string ItemValue { get; set; }
        public CreditApplicationEvent ChangeEvent { get; set; }
        public int? ChangeEventId { get; set; }

        /// <summary>
        /// (i)nsert, (u)pdate, (d)elete
        /// </summary>
        ///
        public string ChangeTypeCode { get; set; }
    }
}