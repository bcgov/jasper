using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using PCSSCommon.Clients.ReportServices;
using Scv.Api.Documents.Strategies;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents.Strategies;

public class ReportStrategyTests : ServiceTestBase
{
    [Fact]
    public async Task ReportStrategyInvoke_ShouldReturnMemoryStream()
    {
        var mockServiceClient = new Mock<ReportServicesClient>(MockBehavior.Strict, this.HttpClient);

        mockServiceClient
            .Setup(s => s.GetCourtListReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(((Stream)new MemoryStream(Encoding.UTF8.GetBytes("Test PDF")), ""));

        var strategy = new ReportStrategy(mockServiceClient.Object);
        var result = await strategy.Invoke(new PdfDocumentRequestDetails
        {
            CourtDivisionCd = "R",
            Date = DateTime.Now,
            LocationId = 1,
            CourtClassCd = "A",
            RoomCode = "RM1",
            AdditionsList = "",
            ReportType = "TYPE"
        });

        Assert.NotNull(strategy);
    }
}
