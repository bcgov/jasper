using System.Collections.Generic;
using Moq;
using Scv.Models.Location;

namespace tests.api.Fixtures;

/// <summary>
/// Extension methods for setting up common LocationService mock behaviors.
/// </summary>
public static class LocationServiceBuilder
{
    public static LocationServiceFixture SetupGetLocations(
        this LocationServiceFixture fixture,
        ICollection<Location> locations = null)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(locations ?? new List<Location>());

        return fixture;
    }

    public static LocationServiceFixture SetupGetLocationShortName(
        this LocationServiceFixture fixture,
        string shortName)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocationShortName(It.IsAny<string>()))
            .ReturnsAsync(shortName);

        return fixture;
    }

    public static LocationServiceFixture SetupGetLocationName(
        this LocationServiceFixture fixture,
        string locationName)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocationName(It.IsAny<string>()))
            .ReturnsAsync(locationName);

        return fixture;
    }

    public static LocationServiceFixture SetupGetLocationCodeFromId(
        this LocationServiceFixture fixture,
        string code)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocationCodeFromId(It.IsAny<string>()))
            .ReturnsAsync(code);

        return fixture;
    }

    public static LocationServiceFixture SetupGetLocationAgencyIdentifier(
        this LocationServiceFixture fixture,
        string agencyIdentifier)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocationAgencyIdentifier(It.IsAny<string>()))
            .ReturnsAsync(agencyIdentifier);

        return fixture;
    }

    public static LocationServiceFixture SetupGetRegionName(
        this LocationServiceFixture fixture,
        string regionName)
    {
        fixture.MockLocationService
            .Setup(s => s.GetRegionName(It.IsAny<string>()))
            .ReturnsAsync(regionName);

        return fixture;
    }

    public static LocationServiceFixture SetupGetLocationCodeByAgencyIdentifierCd(
        this LocationServiceFixture fixture,
        string code)
    {
        fixture.MockLocationService
            .Setup(s => s.GetLocationCodeByAgencyIdentifierCd(It.IsAny<string>()))
            .ReturnsAsync(code);

        return fixture;
    }

    public static LocationServiceFixture VerifyGetLocationsCalled(
        this LocationServiceFixture fixture,
        Times times)
    {
        fixture.MockLocationService.Verify(
            s => s.GetLocations(It.IsAny<bool>()),
            times);

        return fixture;
    }

    public static LocationServiceFixture VerifyGetLocationShortNameCalled(
        this LocationServiceFixture fixture,
        Times times)
    {
        fixture.MockLocationService.Verify(
            s => s.GetLocationShortName(It.IsAny<string>()),
            times);

        return fixture;
    }

    public static LocationServiceFixture VerifyGetRegionNameCalled(
        this LocationServiceFixture fixture,
        Times times)
    {
        fixture.MockLocationService.Verify(
            s => s.GetRegionName(It.IsAny<string>()),
            times);

        return fixture;
    }
}