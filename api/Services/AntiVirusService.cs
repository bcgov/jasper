using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using nClam;

namespace Scv.Api.Services
{
    public interface IAntiVirusService
    {
        Task<(bool isClean, string message)> ScanAsync(Stream fileStream);
    }

    public class ClamAvAntiVirusService(IClamClient clamClient, ILogger<ClamAvAntiVirusService> logger) : IAntiVirusService
    {
        private readonly ILogger<ClamAvAntiVirusService> _logger = logger;

        public async Task<(bool isClean, string message)> ScanAsync(Stream fileStream)
        {
            ClamScanResult scanResult;
            try
            {
                scanResult = await clamClient.SendAndScanFileAsync(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file with ClamAV.");
                return (false, $"Scan error: {ex.Message}");
            }

            return scanResult.Result switch
            {
                ClamScanResults.Clean => (true, "File is clean."),
                ClamScanResults.VirusDetected => (false, $"Virus detected: {scanResult.InfectedFiles?.FirstOrDefault()?.VirusName}"),
                ClamScanResults.Error => (false, $"Scan error: {scanResult.RawResult}"),
                _ => (false, "Unknown scan result.")
            };
        }
    }
}
