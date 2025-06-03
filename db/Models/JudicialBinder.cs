using System.Collections.Generic;

namespace Scv.Db.Models;
public class JudicialBinder : EntityBase
{
    /// <summary>
    /// The User Id of a Judge from User collection.
    /// </summary>
    public string JudgeId { get; set; }
    public string CaseFileId { get; set; }
    public List<JudicialBinderDocument> Documents { get; set; } = [];
}
