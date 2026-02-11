using System.Collections.Generic;
using Newtonsoft.Json;

namespace Scv.Api.Models.Order
{
    public class OrderActionDto
    {
        [JsonProperty("referred_document_id")]
        public int ReferredDocumentId { get; set; }
        [JsonProperty("reviewed_by_agen_id")]
        public int? ReviewedByAgenId { get; set; }
        [JsonProperty("reviewed_by_part_id")]
        public int? ReviewedByPartId { get; set; }
        [JsonProperty("reviewed_by_paas_seq_no")]
        public int? ReviewedByPaasSeqNo { get; set; }
        [JsonProperty("sent_to_agen_id")]
        public int? SentToAgenId { get; set; }
        [JsonProperty("sent_to_part_id")]
        public int? SentToPartId { get; set; }
        [JsonProperty("digital_signature_applied")]
        public bool DigitalSignatureApplied { get; set; }
        [JsonProperty("judicial_action_dt")]
        public string? JudicialActionDt { get; set; }
        [JsonProperty("rejected_dt")]
        public string? RejectedDt { get; set; }
        [JsonProperty("signed_dt")]
        public string? SignedDt { get; set; }
        [JsonProperty("comment_txt")]
        public string? CommentTxt { get; set; }
        [JsonProperty("user_guid")]
        public string? UserGuid { get; set; }
        [JsonProperty("judicial_decision_cd")]
        public string? JudicialDecisionCd { get; set; }
        [JsonProperty("pdf_object")]
        public string? PdfObject { get; set; }

        [JsonProperty("order_terms")]
        public IEnumerable<OrderTerm> OrderTerms { get; set; }
    }
}
