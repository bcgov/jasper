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
    /// <summary>
    /// Id used to identify a court appearance.
    /// </summary>
    public const string APPEARANCE_ID = "appearanceId";
    /// <summary>
    /// Id used to identify a participant.
    /// </summary>
    public const string PARTICIPANT_ID = "participantId";
    /// <summary>
    /// The prof sequence number of a case.
    /// </summary>
    public const string PROF_SEQ_NUMBER = "profSeqNo";
    /// <summary>
    /// The court level code of a case.
    /// </summary>
    public const string COURT_LEVEL_CD = "courtLevelCd";
    /// <summary>
    /// Indicates if the case is criminal.
    /// </summary>
    public const string IS_CRIMINAL = "isCriminal";
    /// <summary>
    /// The id of a case.
    /// </summary>
    public const string CASE_ID = "caseId";
    /// <summary>
    /// The id of a document.
    /// </summary>
    public const string DOCUMENT_ID = "documentId";
    /// <summary>
    /// The division of a case.
    /// </summary>
    public const string DIVISION = "division";
    /// <summary>
    /// The image id of a document.
    /// </summary>
    public const string IMAGE_ID = "imageId";
}
