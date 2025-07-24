using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Jobs;
public class SyncDocumentCategoriesJobTests
{
    private readonly Mock<ILogger<SyncDocumentCategoriesJob>> _logger;
    private readonly Mock<IDocumentCategoryService> _service;

    public SyncDocumentCategoriesJobTests()
    {
        _logger = new Mock<ILogger<SyncDocumentCategoriesJob>>();
        _service = new Mock<IDocumentCategoryService>();
    }

    [Fact]
    public async Task Execute_ShouldSucceed_And_UseDefaultSchedule()
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string)null);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("JobSchedule:SyncDocumentCategories")).Returns(mockSection.Object);

        _service.Setup(s => s.RefreshDocumentCategoriesAsync());

        var job = new SyncDocumentCategoriesJob(_logger.Object, _service.Object, mockConfig.Object);

        await job.Execute();

        Assert.Equal(SyncDocumentCategoriesJob.DEFAULT_SCHEDULE, job.CronSchedule);

        _service.Verify(s => s.RefreshDocumentCategoriesAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldSucceed_And_UseScheduleFromConfig()
    {
        var schedule = "0 0 * * *";
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(schedule);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("JobSchedule:SyncDocumentCategories")).Returns(mockSection.Object);

        _service.Setup(s => s.RefreshDocumentCategoriesAsync());

        var job = new SyncDocumentCategoriesJob(_logger.Object, _service.Object, mockConfig.Object);

        await job.Execute();

        Assert.Equal(schedule, job.CronSchedule);

        _service.Verify(s => s.RefreshDocumentCategoriesAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldHandleException_WhenExecutedFails()
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string)null);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("JobSchedule:SyncDocumentCategories")).Returns(mockSection.Object);

        _service.Setup(s => s.RefreshDocumentCategoriesAsync()).ThrowsAsync(new ArgumentNullException());

        var job = new SyncDocumentCategoriesJob(_logger.Object, _service.Object, mockConfig.Object);

        await job.Execute();

        _service.Verify(s => s.RefreshDocumentCategoriesAsync(), Times.Once);
    }
}
