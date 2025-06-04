namespace Scv.Db.Contants;

/// <summary>
/// Labels to help relate objects (e.g. binder) to a given context such as case detail, court-list, etc.
/// </summary>
public static class LabelConstants
{
    /// <summary>
    /// Id that uniquely identify a case.
    /// </summary>
    public const string PHYSICAL_FILE_ID = "physicalFileId";
    /// <summary>
    /// Identifies the class of case (e.g. Youth, MVA, Enforcement, etc.).
    /// </summary>
    public const string COURT_CLASS_CD = "courtClassCd";
    /// <summary>
    /// Id that uniquely identify the judge id in relation to Users collection.
    /// </summary>
    public const string JUDGE_ID = "judgeId";
}
