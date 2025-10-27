using System;
using System.IO;
using Moq;
using Scv.Api.Models.CourtList;
using static PCSSCommon.Models.ActivityClassUsage;

namespace tests.api.Fixtures;

/// <summary>
/// Extension methods for setting up common CourtListService mock behaviors.
/// </summary>
public static class CourtListServiceBuilder
{
    public static CourtListServiceFixture SetupGetCourtListAppearances(
        this CourtListServiceFixture fixture,
        ActivityAppearanceResultsCollection result = null)
    {
        fixture.MockCourtListService
            .Setup(c => c.GetCourtListAppearances(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(result ?? new ActivityAppearanceResultsCollection());

        return fixture;
    }

    public static CourtListServiceFixture SetupGenerateReport(
        this CourtListServiceFixture fixture,
        Stream stream = null,
        string contentDisposition = "inline; filename=report.pdf")
    {
        fixture.MockCourtListService
            .Setup(c => c.GenerateReportAsync(It.IsAny<CourtListReportRequest>()))
            .ReturnsAsync((stream ?? new MemoryStream(), contentDisposition));

        return fixture;
    }

    public static CourtListServiceFixture SetupGenerateReportWithException(
        this CourtListServiceFixture fixture,
        Exception exception)
    {
        fixture.MockCourtListService
            .Setup(c => c.GenerateReportAsync(It.IsAny<CourtListReportRequest>()))
            .ThrowsAsync(exception);

        return fixture;
    }

    public static CourtListServiceFixture VerifyGetCourtListAppearancesCalled(
        this CourtListServiceFixture fixture,
        Times times)
    {
        fixture.MockCourtListService.Verify(
            c => c.GetCourtListAppearances(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()),
            times);

        return fixture;
    }

    public static CourtListServiceFixture VerifyGenerateReportCalled(
        this CourtListServiceFixture fixture,
        Times times)
    {
        fixture.MockCourtListService.Verify(
            c => c.GenerateReportAsync(It.IsAny<CourtListReportRequest>()),
            times);

        return fixture;
    }
}