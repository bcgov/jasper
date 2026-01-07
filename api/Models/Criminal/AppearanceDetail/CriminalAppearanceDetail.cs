using JCCommon.Clients.FileServices;
using Scv.Api.Helpers;
using Scv.Api.Models.Criminal.Detail;
using Scv.Models.Criminal.AppearanceDetail;
using System.Collections.Generic;


namespace Scv.Api.Models.Criminal.AppearanceDetail;

public class CriminalAppearanceDetail
{
    public string JustinNo { get; set; }
    public string AppearanceId { get; set; }
    public string PartId { get; set; }
    public string AgencyId { get; set; }
    public string ProfSeqNo { get; set; }
    public string CourtRoomCd { get; set; }
    public string FileNumberTxt { get; set; }
    public string AppearanceDt { get; set; }
    public JustinCounsel JustinCounsel { get; set; }
    public CriminalAccused Accused { get; set; }
    public Prosecutor Prosecutor { get; set; }
    public CriminalAdjudicator Adjudicator { get; set; }
    public string JudgesRecommendation { get; set; }
    public string AppearanceNote { get; set; }
    /// <summary>
    /// Extended CriminalAppearanceCount object.
    /// </summary>
    public ICollection<CriminalCharges> Charges { get; set; }

    /// <summary>
    /// Extended CriminalAppearanceMethod object.
    /// </summary>
    public ICollection<Scv.Models.Criminal.AppearanceDetail.CriminalAppearanceMethod> AppearanceMethods { get; set; }
    public string EstimatedTimeHour { get; set; }
    public string EstimatedTimeMin { get; set; }

    public CriminalFileDetailResponseCourtLevelCd CourtLevelCd { get; set; }
}