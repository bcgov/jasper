using System.Collections.Generic;

namespace Scv.Api.Models.CourtList;

public class CourtListDocumentBundleRequest
{
    public List<CourtListAppearanceDocumentRequest> Appearances { get; set; }
}
