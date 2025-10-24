using JCCommon.Clients.FileServices;
using Mapster;
using Newtonsoft.Json;

namespace Scv.Models.Criminal.Detail
{
    /// <summary>
    /// Some fields hidden, a few extra fields.
    /// </summary>
    public class CriminalCount : CfcAppearanceCount
    {
        public string PartId { get; set; }
        public string AppearanceDate { get; set; }
        public string FindingDsc { get; set; }

        [AdaptIgnore]
        [JsonIgnore]
        public new string AppcId { get; set; }

        [AdaptIgnore]
        [JsonIgnore]
        public new string AppearanceReason { get; set; }

        public new System.Collections.Generic.ICollection<CriminalSentence> Sentence { get; set; }
    }
}