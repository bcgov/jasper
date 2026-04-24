namespace Scv.Models.Document
{
    public static class DocumentCategories
    {
        public const string AFFIDAVITS = "AFFIDAVITS";
        public const string BAIL = "BAIL";
        public const string INITIATING = "INITIATING";
        public const string MOTIONS = "MOTIONS";
        public const string ORDERS = "ORDERS";
        public const string PLEADINGS = "PLEADINGS";
        public const string PSR = "PSR";
        public const string REPORT = "REPORT";
        public const string ROP = "ROP";
        public const string CSR = "CSR";
        public const string LITIGANT = "LITIGANT";

        public static readonly List<string> ALL_DOCUMENT_CATEGORIES = [
        AFFIDAVITS,
        BAIL,
        INITIATING,
        MOTIONS,
        ORDERS,
        PLEADINGS,
        PSR,
        LITIGANT,
        CSR
];

        public static readonly List<string> KEY_DOCUMENT_CATEGORIES = [
            INITIATING,
        ROP,
        PSR,
        REPORT
        ];
    }
}
