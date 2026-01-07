using JCCommon.Clients.FileServices;
using Mapster;
using Newtonsoft.Json;
using Scv.Models.Civil.Detail;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Scv.Models.Civil.AppearanceDetail
{
    /// <summary>
    /// This includes extra fields that our API doesn't give us.
    /// This excludes appearances, because this is used under the context of looking up an appearance details (meaning you already have the appearance to target).
    /// </summary>
    public class CivilAppearanceDocument : CvfcDocument3
    {
        public string Category { get; set; }
        public string DocumentTypeDescription { get; set; }
        /// <summary>
        /// Modified class, hides fields.
        /// </summary>
        public new ICollection<CivilIssue> Issue { get; set; }

        /// <summary>
        /// Exclude this property, even though it exists in CvfcDocument3.
        /// </summary>
        [JsonIgnore]
        [AdaptIgnore]
        public new ICollection<CvfcAppearance> Appearance { get; }

        [JsonIgnore]
        [AdaptIgnore]
        public new string LastAppearanceId { get; }

        [JsonIgnore]
        [AdaptIgnore]
        public new string LastAppearanceDt { get; }

        [JsonIgnore]
        [AdaptIgnore]
        public new string LastAppearanceTm { get; }
    }
}