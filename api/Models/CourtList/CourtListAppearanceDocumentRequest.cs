using JCCommon.Clients.FileServices;

namespace Scv.Api.Models.CourtList;

public class CourtListAppearanceDocumentRequest
{
    public string FileId { get; set; }
    public string AppearanceId { get; set; }
    public string ParticipantId { get; set; }
    public CourtClassCd CourtClassCd { get; set; }
}
