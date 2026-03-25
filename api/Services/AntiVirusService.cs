using System.IO;
using System.Linq;
using System.Threading.Tasks;
using nClam;

namespace Scv.Api.Services
{
    public interface IAntiVirusService
    {
        Task<(bool isClean, string message)> ScanAsync(Stream fileStream);
    }

    public class ClamAvAntiVirusService(IClamClient clamClient) : IAntiVirusService
    {
        public async Task<(bool isClean, string message)> ScanAsync(Stream fileStream)
        {
            var scanResult = await clamClient.SendAndScanFileAsync(fileStream);

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
