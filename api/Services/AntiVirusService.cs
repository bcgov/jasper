using Scv.Api.Infrastructure.ClamAv;
using System.IO;
using System.Threading.Tasks;

namespace Scv.Api.Services
{
    public interface IAntiVirusService
    {
        Task<(bool isClean, string message)> ScanAsync(Stream fileStream);
    }

    public class ClamAvAntiVirusService(IClamAvClient clamAvClient) : IAntiVirusService
    {
        public async Task<(bool isClean, string message)> ScanAsync(Stream fileStream)
        {
            var result = await clamAvClient.ScanAsync(fileStream);

            return result.Status switch
            {
                ClamAvScanStatus.Clean => (true, "File is clean."),
                ClamAvScanStatus.VirusDetected => (false, $"Virus detected: {result.VirusName}"),
                ClamAvScanStatus.Error => (false, $"Scan error: {result.RawResponse}"),
                _ => (false, "Unknown scan result.")
            };
        }
    }
}
