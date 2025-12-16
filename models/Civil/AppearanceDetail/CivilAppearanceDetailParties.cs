using Scv.Models.Civil.AppearanceDetail;

namespace Scv.Api.Models.Civil.AppearanceDetail;

public class CivilAppearanceDetailParties
{
    public string AppearanceId { get; set; }
    public ICollection<CivilAppearanceDetailParty> Party { get; set; }
}