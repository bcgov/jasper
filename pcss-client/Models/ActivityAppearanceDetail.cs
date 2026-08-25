namespace PCSSCommon.Models;

public partial class ActivityClassUsage
{
    public partial class PcssCounsel
    {
        public partial class ActivityAppearanceDetail
        {
            public List<string> CounselNames
            {
                get
                {
                    if (CounselNameDescriptor.IsSelfRepresented(SelfRepresentedYn))
                    {
                        return [CounselNameDescriptor.SELF_REPRESENTED];
                    }

                    if (Counsel is { Count: > 0 })
                    {
                        return [.. Counsel.Where(c => c != null).Select(CounselNameDescriptor.FullName)];
                    }

                    if (JustinCounsel is { } justinCounsel)
                    {
                        return [$"{justinCounsel.GivenNm} {justinCounsel.LastNm}".Trim()];
                    }

                    if (CeisCounsel is { } ceisCounsel)
                    {
                        return [.. ceisCounsel.Where(c => c != null).Select(c => c.FullNm)];
                    }

                    return [];
                }
            }
        }
    }
}
