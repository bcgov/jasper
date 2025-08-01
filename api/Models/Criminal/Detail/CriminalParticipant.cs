using System.Collections.Generic;
using JCCommon.Clients.FileServices;
using Scv.Api.Helpers.Documents;

namespace Scv.Api.Models.Criminal.Detail;
/// <summary>
/// Extends JCCommon.Clients.FileServices.CriminalParticipant
/// </summary>
public class CriminalParticipant : JCCommon.Clients.FileServices.CriminalParticipant
{
    public CriminalParticipant()
    {
        Count = [];
        Ban = [];
    }

    public string FullName => GivenNm != null && LastNm != null
        ? $"{GivenNm?.Trim()} {LastNm?.Trim()}"
        : OrgNm;

    /// <summary>
    /// Custom class to extend.
    /// </summary>
    public ICollection<CriminalDocument> Document { get; set; }

    public IEnumerable<CriminalDocument> KeyDocuments => KeyDocumentResolver.GetCriminalKeyDocuments(Document);

    /// <summary>
    /// Can only be set to true, cannot be set to false and have the fields reappear.
    /// </summary>
    public bool? HideJustinCounsel
    {
        get => _hideJustinCounsel;
        set
        {
            _hideJustinCounsel = value;
            if (value.HasValue && value.Value)
            {
                CounselLastNm = null;
                CounselGivenNm = null;
                CounselEnteredDt = null;
                CounselPartId = null;
                CounselRelatedRepTypeCd = null;
                CounselRrepId = null;
            }
        }
    }

    /// <summary>
    /// Slimmed down version of CfcAppearanceCount.
    /// </summary>
    public List<CriminalCount> Count { get; set; }

    /// <summary>
    /// Extended, with PartId.
    /// </summary>
    public List<CriminalBan> Ban { get; set; }

    public ICollection<ClAgeNotice> AgeNotice { get; set; }

    private bool? _hideJustinCounsel;
}