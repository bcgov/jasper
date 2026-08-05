namespace PCSSCommon.Models;

/// <summary>
/// CriminalFileDetail has been stripped-down to only the properties JASPER needs.
/// </summary>
public class CriminalFileDetail
{
    public List<Participant> Participant { get; set; } = [];
}

public partial class Participant
{
    public string PartId { get; set; }
    public string SelfRepresentedYn { get; set; }
    public Counsel Counsel { get; set; }
    public JustinCounsel JustinCounsel { get; set; }
}

public class JustinCounsel
{
    public string LastNm { get; set; }
    public string GivenNm { get; set; }
}

public class Counsel
{
    public string LastNm { get; set; }
    public string GivenNm { get; set; }
    public string PrefNm { get; set; }
    public string OrgNm { get; set; }
}
