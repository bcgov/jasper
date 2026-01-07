using JCCommon.Clients.FileServices;

namespace Scv.Models.Search
{
    public class FilesCivilQuery
    {
        public SearchMode SearchMode { get; set; }
        public string FileHomeAgencyId { get; set; }
        public string FileNumber { get; set; }
        public string FilePrefix { get; set; }
        public string FilePermissions { get; set; }
        public string FileSuffixNumber { get; set; }
        public string MDocReferenceTypeCode { get; set; }
        public CourtClassCd? CourtClass { get; set; }
        public CourtLevelCd? CourtLevel { get; set; }
        public NameSearchTypeCd? NameSearchType { get; set; }
        public string LastName { get; set; }
        public string OrgName { get; set; }
        public string GivenName { get; set; }
        public DateTime? Birth { get; set; }
        public string SearchByCrownPartId { get; set; }
        public SearchByCrownActiveOnlyYN? SearchByCrownActiveOnly { get; set; }
        public SearchByCrownFileDesignationCd? SearchByCrownFileDesignation { get; set; }
        public string MdocJustinNumberSet { get; set; }
        public string PhysicalFileIdSet { get; set; }
    }
}