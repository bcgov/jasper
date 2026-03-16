namespace Scv.Models.Binder {

    /// <summary>
    /// Criteria for searching binders with complex queries.
    /// </summary>
    public class SearchBindersCriteria
    {
        /// <summary>
        /// Label keys that must exist (e.g., "JUDGE_ID" to find all judicial binders).
        /// </summary>
        public List<string> LabelKeysExist { get; set; } = [];

        /// <summary>
        /// Label key-value pairs that must match exactly.
        /// </summary>
        public Dictionary<string, string> LabelMatches { get; set; } = new();

        /// <summary>
        /// Filter binders updated before this date/time.
        /// </summary>
        public DateTime? UpdatedBefore { get; set; }

        /// <summary>
        /// Maximum number of results to return. Default is 100. Set to null for unlimited.
        /// </summary>
        public int? Limit { get; set; } = 100;

        /// <summary>
        /// Number of results to skip (for pagination).
        /// </summary>
        public int? Skip { get; set; }
    }
}
