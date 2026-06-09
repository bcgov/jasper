namespace Scv.Models.Order;

public static class PriorityTypeDescriptor
{
    public const string PROTECTION_ORDER_TYPE = "PRO";
    public const string COURT_DIRECTED_ORDER_TYPE = "CRTD";
    public const string OTHER_ORDER_TYPE = "OTHR";

    public static readonly string[] PRIORITY_ORDERS =
    [
        PROTECTION_ORDER_TYPE,
        COURT_DIRECTED_ORDER_TYPE,
        OTHER_ORDER_TYPE
    ];

    public static string Describe(string priorityType) => priorityType switch
    {
        PROTECTION_ORDER_TYPE => "Protection Orders",
        COURT_DIRECTED_ORDER_TYPE => "Court Directed",
        OTHER_ORDER_TYPE => "Other",
        _ => string.Empty
    };

    public static bool IsPriority(string priorityType) =>
        PRIORITY_ORDERS.Contains(priorityType, StringComparer.OrdinalIgnoreCase);
}
