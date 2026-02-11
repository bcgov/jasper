namespace Scv.Api.Models.Order
{
    public class OrderTerm
    {
        [Newtonsoft.Json.JsonProperty("term_seq_no")]
        public int TermSeqNo { get; set; }
        [Newtonsoft.Json.JsonProperty("term_txt")]
        public string TermTxt { get; set; }
        [Newtonsoft.Json.JsonProperty("term_display_sort_no")]
        public int TermDisplaySortNo { get; set; }
    }
}
