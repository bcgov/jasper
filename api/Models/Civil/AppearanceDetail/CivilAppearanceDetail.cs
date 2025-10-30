using System.Collections.Generic;
using JCCommon.Clients.FileServices;
using Scv.Api.Models.Civil.Detail;

namespace Scv.Api.Models.Civil.AppearanceDetail
{
    public class CivilAppearanceDetail
    {
        public string PhysicalFileId { get; set; }
        public string AgencyId { get; set; }
        public string AppearanceId { get; set; }
        public string CourtRoomCd { get; set; }
        public string FileNumberTxt { get; set; }
        public string AppearanceDt { get; set; }
        public string AppearanceResultCd { get; set; }
        public string AppearanceResultDesc { get; set; }
        public string AppearanceReasonCd { get; set; }
        public string AppearanceReasonDesc { get; set; }
        public CivilAdjudicator Adjudicator { get; set; }

        /// <summary>
        /// Extended object. 
        /// </summary>
        public ICollection<CivilAppearanceDetailParty> Party { get; set; }

        /// <summary>
        /// Extended document object.
        /// </summary>
        public ICollection<CivilAppearanceDocument> Document { get; set; }

        /// <summary>
        /// Extended object.
        /// </summary>
        public ICollection<CivilAppearanceMethod> AppearanceMethod { get; set; }
        public CivilFileDetailResponseCourtLevelCd CourtLevelCd { get; set; }

        /// <summary>
        /// Current user's binder documents.
        /// </summary>
        public IList<CivilDocument> BinderDocuments { get; set; }
    }
}