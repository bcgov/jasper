using System.Collections.Generic;

namespace Scv.Api.Models.Civil.AppearanceDetail;

public class CivilAppearanceDetailMethods
{
    public string AppearanceId { get; set; }
    public ICollection<CivilAppearanceMethod> AppearanceMethod { get; set; }
}