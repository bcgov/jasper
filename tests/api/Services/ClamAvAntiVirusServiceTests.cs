using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using nClam;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class ClamAvAntiVirusServiceTests
{
    private readonly Mock<IClamClient> _mockClamClient;
    private readonly Mock<ILogger<ClamAvAntiVirusService>> _mockLogger;
    private readonly ClamAvAntiVirusService _service;

    public ClamAvAntiVirusServiceTests()
    {
        _mockClamClient = new Mock<IClamClient>();
        _mockLogger = new Mock<ILogger<ClamAvAntiVirusService>>();
        _service = new ClamAvAntiVirusService(_mockClamClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ScanAsync_ReturnsTrue_WhenFileIsClean()
    {
        var scanResult = new ClamScanResult("stream: OK");
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ReturnsAsync(scanResult);

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.True(isClean);
        Assert.Equal("File is clean.", message);
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenVirusIsDetected()
    {
        var virusName = "Eicar-Test-Signature";
        var scanResult = new ClamScanResult($"stream: {virusName} FOUND");
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ReturnsAsync(scanResult);

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Contains(virusName, message);
        Assert.StartsWith("Virus detected:", message);
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenScanReturnsError()
    {
        var rawResult = "stream: lstat() failed. ERROR";
        var scanResult = new ClamScanResult(rawResult);
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ReturnsAsync(scanResult);

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Equal($"Scan error: {rawResult}", message);
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenScanResultIsUnknown()
    {
        var scanResult = new ClamScanResult("stream: UNEXPECTED_STATUS");
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ReturnsAsync(scanResult);

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Equal("Unknown scan result.", message);
    }

    [Fact]
    public async Task ScanAsync_ReturnsNullVirusName_WhenInfectedFilesIsEmpty()
    {
        var scanResult = new ClamScanResult("FOUND");
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ReturnsAsync(scanResult);

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Equal("Virus detected: ", message);
    }

    #region Exception handling

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenSocketExceptionIsThrown()
    {
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new SocketException());

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.StartsWith("Scan error:", message);
        VerifyLogError();
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenIOExceptionIsThrown()
    {
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new IOException("Connection dropped"));

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Equal("Scan error: Connection dropped", message);
        VerifyLogError();
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenInvalidOperationExceptionIsThrown()
    {
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new InvalidOperationException("Stream not readable"));

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.Equal("Scan error: Stream not readable", message);
        VerifyLogError();
    }

    [Fact]
    public async Task ScanAsync_ReturnsFalse_WhenObjectDisposedExceptionIsThrown()
    {
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new ObjectDisposedException("stream"));

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.StartsWith("Scan error:", message);
        VerifyLogError();
    }

    [Fact]
    public async Task ScanAsync_PropagatesException_WhenUnexpectedExceptionIsThrown()
    {
        // Verifies the `when` filter lets non-communication exceptions propagate
        _mockClamClient
            .Setup(c => c.SendAndScanFileAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new OutOfMemoryException());

        var (isClean, message) = await _service.ScanAsync(new MemoryStream());

        Assert.False(isClean);
        Assert.StartsWith("Scan error:", message);
        VerifyLogError();
    }

    #endregion

    #region Helpers

    private void VerifyLogError() =>
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

    #endregion
}
