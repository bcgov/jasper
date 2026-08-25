using Bogus;
using PCSSCommon.Models;
using Xunit;
using static PCSSCommon.Models.ActivityClassUsage;

namespace tests.pcss_client.Models;

public class CounselNameDescriptorTests
{
    private readonly Faker _faker = new();

    [Theory]
    [InlineData("Y", true)]
    [InlineData("y", true)]
    [InlineData("N", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Yes", false)]
    public void IsSelfRepresented_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, CounselNameDescriptor.IsSelfRepresented(input));
    }

    [Fact]
    public void ResolveName_PrefersPrefNm_OverEverything()
    {
        var prefNm = _faker.Name.FullName();
        var counsel = new Counsel
        {
            PrefNm = prefNm,
            OrgNm = _faker.Company.CompanyName(),
            GivenNm = _faker.Name.FirstName(),
            LastNm = _faker.Name.LastName()
        };

        var (given, last) = CounselNameDescriptor.ResolveName(counsel);

        Assert.Equal(string.Empty, given);
        Assert.Equal(prefNm, last);
    }

    [Fact]
    public void ResolveName_UsesOrgNm_WhenPrefNmMissing()
    {
        var orgNm = _faker.Company.CompanyName();
        var counsel = new Counsel
        {
            PrefNm = "   ",
            OrgNm = orgNm,
            GivenNm = _faker.Name.FirstName(),
            LastNm = _faker.Name.LastName()
        };

        var (given, last) = CounselNameDescriptor.ResolveName(counsel);

        Assert.Equal(string.Empty, given);
        Assert.Equal(orgNm, last);
    }

    [Fact]
    public void ResolveName_FallsBackToPersonName_WhenPrefAndOrgMissing()
    {
        var givenNm = _faker.Name.FirstName();
        var lastNm = _faker.Name.LastName();
        var counsel = new Counsel
        {
            PrefNm = null,
            OrgNm = "",
            GivenNm = givenNm,
            LastNm = lastNm
        };

        var (given, last) = CounselNameDescriptor.ResolveName(counsel);

        Assert.Equal(givenNm, given);
        Assert.Equal(lastNm, last);
    }

    [Fact]
    public void FullName_CombinesGivenAndLast()
    {
        var givenNm = _faker.Name.FirstName();
        var lastNm = _faker.Name.LastName();
        var counsel = new Counsel { GivenNm = givenNm, LastNm = lastNm };

        Assert.Equal($"{givenNm} {lastNm}", CounselNameDescriptor.FullName(counsel));
    }

    [Fact]
    public void FullName_TrimsWhenGivenNameIsEmpty()
    {
        var prefNm = _faker.Name.FullName();
        var counsel = new Counsel { PrefNm = prefNm };

        Assert.Equal(prefNm, CounselNameDescriptor.FullName(counsel));
    }

    [Fact]
    public void FullName_UsesOrgName_WithoutLeadingSpace()
    {
        var orgNm = _faker.Company.CompanyName();
        var counsel = new Counsel { OrgNm = orgNm };

        Assert.Equal(orgNm, CounselNameDescriptor.FullName(counsel));
    }

    [Fact]
    public void ResolveName_PcssCounsel_PrefersPrefNm()
    {
        var prefNm = _faker.Name.FullName();
        var counsel = new PcssCounsel
        {
            PrefNm = prefNm,
            OrgNm = _faker.Company.CompanyName(),
            GivenNm = _faker.Name.FirstName(),
            LastNm = _faker.Name.LastName()
        };

        var (given, last) = CounselNameDescriptor.ResolveName(counsel);

        Assert.Equal(string.Empty, given);
        Assert.Equal(prefNm, last);
    }

    [Fact]
    public void FullName_PcssCounsel_CombinesPersonName()
    {
        var givenNm = _faker.Name.FirstName();
        var lastNm = _faker.Name.LastName();
        var counsel = new PcssCounsel { GivenNm = givenNm, LastNm = lastNm };

        Assert.Equal($"{givenNm} {lastNm}", CounselNameDescriptor.FullName(counsel));
    }
}
