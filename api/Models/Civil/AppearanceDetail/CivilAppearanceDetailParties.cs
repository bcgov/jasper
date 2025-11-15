using System.Collections.Generic;

namespace Scv.Api.Models.Civil.AppearanceDetail;

public class CivilAppearanceDetailParties
{
    public string AppearanceId { get; set; }
    public ICollection<CivilAppearanceDetailParty> Party { get; set; }
}