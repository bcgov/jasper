using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using nClam;
using Scv.Api.Infrastructure.HealthChecks;
using Xunit;

namespace tests.api.Infrastructure.HealthChecks;

public class ClamAvHealthCheckTests
{
    private readonly Mock<IClamClient> _mockClamClient;
    private readonly ClamAvHealthCheck _healthCheck;

    // Matches the format expected by the health check's regex and date parser
    private const string VersionTemplate = "ClamAV 1.4.2/27521/{0}";
    private const string DateFormat = "ddd MMM d HH:mm:ss yyyy";

    public ClamAvHealthCheckTests()
    {
        _mockClamClient = new Mock<IClamClient>();
        _healthCheck = new ClamAvHealthCheck(_mockClamClient.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDaemonIsUnreachable()
    {
        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("ClamAV daemon is unreachable.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenVersionIsNull()
    {
        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClamClient
            .Setup(c => c.GetVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Unable to parse ClamAV version string", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenVersionStringDoesNotMatchExpectedFormat()
    {
        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClamClient
            .Setup(c => c.GetVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-a-valid-clam-version");

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Unable to parse ClamAV version string", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenDefinitionsAreUpToDate()
    {
        // definitions updated 12 hours ago, within the 1-day threshold
        var recentDate = DateTimeOffset.UtcNow.AddHours(-12);
        var version = BuildVersionString(recentDate);

        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClamClient
            .Setup(c => c.GetVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("ClamAV OK", result.Description);
        Assert.Contains("1.4.2", result.Description);
        Assert.Contains("27521", result.Description);
        Assert.True(result.Data.ContainsKey("dbDate"));
        Assert.True(result.Data.ContainsKey("ageInDays"));
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenDefinitionsAreTooOld()
    {
        // definitions updated 3 days ago, exceeds the 1-day threshold
        var oldDate = DateTimeOffset.UtcNow.AddDays(-3);
        var version = BuildVersionString(oldDate);

        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClamClient
            .Setup(c => c.GetVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("day(s) old", result.Description);
        Assert.Contains("freshclamd", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenExceptionIsThrown()
    {
        _mockClamClient
            .Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection refused"));

        var result = await _healthCheck.CheckHealthAsync(null!);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("threw an exception", result.Description);
        Assert.NotNull(result.Exception);
        Assert.Equal("Connection refused", result.Exception.Message);
    }

    #region Helpers

    private static string BuildVersionString(DateTimeOffset date) =>
        string.Format(VersionTemplate, date.ToString(DateFormat, CultureInfo.InvariantCulture));

    #endregion Helpers
}
