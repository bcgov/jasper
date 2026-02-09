using System;
using Newtonsoft.Json;
using Scv.Db.Models;

namespace Scv.Api.Models.Order;

public class ReviewedOrderDto
{
    [JsonProperty("referred_document_id")]
    public int ReferredDocumentId { get; set; }
    [JsonProperty("judicial_action_dt")]
    public DateTime JudicialActionDt { get; set; }
    [JsonProperty("comment_txt")]
    public string CommentTxt { get; set; }
    [JsonProperty("reviewed_by_agen_id")]
    public int? ReviewedByAgenId { get; set; }
    [JsonProperty("reviewed_by_part_id")]
    public int? ReviewedByPartId { get; set; }
    [JsonProperty("reviewed_by_paas_seq_no")]
    public int? ReviewedByPassSeqNo { get; set; }
    [JsonProperty("user_guid")]
    public Guid UserGuid { get; set; }
    [JsonProperty("judicial_decision_cd")]
    public JudicialDecisionCode JudicialDecisionCd { get; set; }
    [JsonProperty("digital_signature_applied")]
    public bool DigitalSignatureApplied { get; set; }
    [JsonProperty("rejected_dt")]
    public DateTime? RejectedDt { get; set; }
    [JsonProperty("signed_dt")]
    public DateTime? SignedDt { get; set; }
    [JsonProperty("pdf_object")]
    public string PdfObject { get; set; }
    // OrderTerms to be included at a later point
}