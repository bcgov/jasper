using System.Collections.Generic;

namespace Scv.Api.Models.Civil.Detail;

public class JudicialBinderDto : BaseDto
{
    /// <summary>
    /// The User Id of a Judge from User collection.
    /// </summary>
    public string JudgeId { get; set; }
    public string CaseFileId { get; set; }
    public List<JudicialBinderDocumentDto> Documents { get; set; }
}
