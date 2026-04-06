using JCCommon.Clients.FileServices;

namespace Scv.Models.Search
{
    public class FilesCriminalQuery
    {
        public string RequestAgencyIdentifierId { get; set; }
        public string RequestPartId { get; set; }
        public string ApplicationCd { get; set; }
        public SearchMode SearchMode { get; set; }
        public string FileHomeAgencyId { get; set; }
        public string FileNumberTxt { get; set; }
        public string FilePrefixTxt { get; set; }
        public string FilePermissions { get; set; }
        public string FileSuffixNo { get; set; }
        public string MdocRefTypeCode { get; set; }
        public CourtClassCd? CourtClass { get; set; }
        public CourtLevelCd? CourtLevel { get; set; }
        public NameSearchTypeCd? NameSearchTypeCd { get; set; }
        public string LastName { get; set; }
        public string OrgName { get; set; }
        public string GivenName { get; set; }
        public DateTime? Birth { get; set; }
        public string SearchByCrownPartId { get; set; }
        public SearchByCrownActiveOnlyYN? SearchByCrownActiveOnly { get; set; }
        public SearchByCrownFileDesignationCd? SearchByCrownFileDesignation { get; set; }
        public string MdocJustinNoSet { get; set; }
        public string PhysicalFileIdSet { get; set; }
    }
}