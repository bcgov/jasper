namespace Scv.Models.Order;

public static class CourtListTypeDescriptor
{
    public const string SMALL_CLAIMS_COURT_LIST_TYPE = "PSC";
    public const string FAMILY_COURT_LIST_TYPE = "PFA";
    public const string PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE = "PSM";
    public const string PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE = "PFM";

    public const string ORDER_DESCRIPTION = "Order";
    public const string DESK_ORDER_DESCRIPTION = "Desk Order";

    public static string Describe(string courtListType)
    {
        return courtListType switch
        {
            SMALL_CLAIMS_COURT_LIST_TYPE or FAMILY_COURT_LIST_TYPE => ORDER_DESCRIPTION,
            PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE or PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE => DESK_ORDER_DESCRIPTION,
            _ => courtListType,
        };
    }
}
