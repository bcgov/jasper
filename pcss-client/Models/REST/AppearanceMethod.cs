using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class AppearanceMethod
    {
        public AppearanceMethod()
        {
            Details = new List<AppearanceMethodDetail>();
        }
        public string ResponseMessageTxt { get; set; }
        public string ResponseCd { get; set; }
        public string CourtDivisionCd { get; set; }
        public List<AppearanceMethodDetail> Details { get; set; }
    }
    
    public class AppearanceMethodDetail
    {
        /* Both*/
        public string AppearanceId { get; set; }        
        /* Both*/
        public string RoleTypeCd { get; set; }
        /* Both */
        public string AppearanceMethodCd { get; set; }
        
        /* Criminal Only */
        public string AssetUsageSeqNo { get; set; }
        /* Criminal Only */
        public string PhoneNumberTxt { get; set; }
        /* Criminal Only */
        public string InstructionTxt { get; set; }
        /* Criminal Only */
        public string ApprMethodCcn { get; set; }
        
        /* Civil Only */
        public string OrigRoleCd { get; set; }
        /* Civil Only */
        public string OrigAppearanceMethodCd { get; set; }
    }

    /// <summary>
    /// A flattened version of the model for saving and updating since the front end only supports one at a time
    /// </summary>
    public class AppearanceMethodSaveAndUpdateModel : AppearanceMethodDetail
    {        
        public string CourtDivisionCd { get; set; }
    }

    public class AssetType {
       public string AssetTypeCd { get; set; }
       public string AssetTypeDsc { get; set; }
    }

    public class AppearanceMethodAllowedCombo
    {
        public AppearanceMethodAllowedCombo()
        {
            AssetTypes = new List<AssetType>();
        }
        public string ParticipantRoleTypeCd { get; set; }
        public string ParticipantRoleTypeDsc { get; set; }
        public List<AssetType> AssetTypes { get; set; }
    }
}