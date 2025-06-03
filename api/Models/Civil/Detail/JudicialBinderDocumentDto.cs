namespace Scv.Api.Models.Civil.Detail;

public class JudicialBinderDocumentDto
{
    public string CivilDocumentId { get; set; }
    public int Order { get; set; }
    public CivilDocument Document { get; set; }
}
