namespace PCSSCommon.Models;

/// <summary>
/// CivilFileDetail has been stripped-down to only the properties JASPER needs.
/// </summary>
public class CivilFileDetail
{
    public List<CivilParty> Party { get; set; }
}

public class CivilParty
{
    public string PartyId { get; set; }
    public string SelfRepresentedYn { get; set; }
    public Counsel Counsel { get; set; }
    public List<CeisCounsel> CeisCounsel { get; set; }
}

public class CeisCounsel
{
    public string FullNm { get; set; }
}
