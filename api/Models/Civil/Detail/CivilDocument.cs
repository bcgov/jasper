using JCCommon.Clients.FileServices;
using Scv.Db.Models;
using Scv.Models.Civil.Detail;
using System.Collections.Generic;

namespace Scv.Api.Models.Civil.Detail
{

    /// <summary>
    /// This includes extra fields that our API doesn't give us.
    /// </summary>
    public class CivilDocument : CvfcDocument3
    {
        private string _category;

        public string Category
        {
            get => _category;
            set => _category = DocumentCategory.Format(value);
        }
        public string DocumentTypeDescription { get; set; }
        public string NextAppearanceDt { get; set; }
        public ICollection<ClFiledBy> FiledBy { get; set; }

        /// <summary>
        /// Hides fields for issue. 
        /// </summary>
        public new ICollection<CivilIssue> Issue { get; set; }
    }
}