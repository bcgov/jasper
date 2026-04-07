using System.IO;
using System.Threading.Tasks;
using Moq;
using nClam;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class ClamAvAntiVirusServiceTests
{
    private readonly Mock<IClamClient> _mockClamClient;
    private readonly ClamAvAntiVirusService _service;

    public ClamAvAntiVirusServiceTests()
    {
        _mockClamClient = new Mock<IClamClient>();
        _service = new ClamAvAntiVirusService(_mockClamClient.Object);
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
}