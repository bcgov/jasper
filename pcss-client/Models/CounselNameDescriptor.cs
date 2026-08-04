using static PCSSCommon.Models.ActivityClassUsage;

namespace PCSSCommon.Models;

public static class CounselNameDescriptor
{
    public const string SELF_REPRESENTED = "Self-Represented";

    public static bool IsSelfRepresented(string selfRepresentedYn) =>
        string.Equals(selfRepresentedYn, "Y", StringComparison.OrdinalIgnoreCase);

    public static (string givenName, string lastName) ResolveName(Counsel counsel)
    {
        return Resolve(counsel.PrefNm, counsel.OrgNm, counsel.GivenNm, counsel.LastNm);
    }

    public static (string givenName, string lastName) ResolveName(PcssCounsel counsel)
    {
        return Resolve(counsel.PrefNm, counsel.OrgNm, counsel.GivenNm, counsel.LastNm);
    }

    public static string FullName(Counsel counsel)
    {
        var (givenName, lastName) = ResolveName(counsel);
        return $"{givenName} {lastName}".Trim();
    }

    public static string FullName(PcssCounsel counsel)
    {
        var (givenName, lastName) = ResolveName(counsel);
        return $"{givenName} {lastName}".Trim();
    }


    /// <summary>
    // Mirrors PCSS's precedence for determining counsel name: PrefNm -> OrgNm -> person name.
    /// </summary>
    /// <param name="prefName">The preferred name of the counsel.</param>
    /// <param name="orgName">The organization name of the counsel.</param>
    /// <param name="givenName">The given name of the counsel.</param>
    /// <param name="lastName">The last name of the counsel.</param>
    /// <returns>A tuple containing the resolved given name and last name.</returns>
    private static (string givenName, string lastName) Resolve(string prefName, string orgName, string givenName, string lastName)
    {
        if (!string.IsNullOrWhiteSpace(prefName))
        {
            return (string.Empty, prefName);
        }
        if (!string.IsNullOrWhiteSpace(orgName))
        {
            return (string.Empty, orgName);
        }
        return (givenName, lastName);
    }
}
