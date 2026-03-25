using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.ClamAv
{
    /// <summary>
    /// Communicates with a clamd daemon using the ClamAV INSTREAM protocol over TCP.
    /// Protocol: https://linux.die.net/man/8/clamd — no third-party library required.
    /// </summary>
    public interface IClamAvClient
    {
        Task<ClamAvScanResult> ScanAsync(Stream fileStream, CancellationToken cancellationToken = default);
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
        Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);
    }

    public class ClamAvClient(string host, int port, int chunkSize = 4096) : IClamAvClient
    {
        private const string PingCommand = "zPING\0";
        private const string VersionCommand = "zVERSION\0";
        private const string InstreamCommand = "zINSTREAM\0";

        public async Task<ClamAvScanResult> ScanAsync(Stream fileStream, CancellationToken cancellationToken = default)
        {
            using var tcp = await ConnectAsync(cancellationToken);
            await using var networkStream = tcp.GetStream();

            // Send INSTREAM command
            var command = Encoding.ASCII.GetBytes(InstreamCommand);
            await networkStream.WriteAsync(command, cancellationToken);

            // Stream file in chunks: [4-byte big-endian length][chunk data]
            var buffer = new byte[chunkSize];
            var lengthPrefix = new byte[4];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(lengthPrefix, (uint)bytesRead);
                await networkStream.WriteAsync(lengthPrefix, cancellationToken);
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            // Terminate stream with 4 zero bytes
            await networkStream.WriteAsync(new byte[4], cancellationToken);
            await networkStream.FlushAsync(cancellationToken);

            var response = await ReadResponseAsync(networkStream, cancellationToken);
            return ParseScanResponse(response);
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var tcp = await ConnectAsync(cancellationToken);
                await using var stream = tcp.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes(PingCommand), cancellationToken);
                var response = await ReadResponseAsync(stream, cancellationToken);
                return response.Trim() == "PONG";
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            using var tcp = await ConnectAsync(cancellationToken);
            await using var stream = tcp.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes(VersionCommand), cancellationToken);
            return (await ReadResponseAsync(stream, cancellationToken)).Trim('\0', '\n');
        }

        private async Task<TcpClient> ConnectAsync(CancellationToken cancellationToken)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cancellationToken);
            return tcp;
        }

        private static async Task<string> ReadResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();
            var buf = new byte[256];
            int read;
            while ((read = await stream.ReadAsync(buf, cancellationToken)) > 0)
            {
                await ms.WriteAsync(buf.AsMemory(0, read), cancellationToken);
                if (!stream.DataAvailable)
                    break;
            }
            return Encoding.ASCII.GetString(ms.ToArray());
        }

        private static ClamAvScanResult ParseScanResponse(string response)
        {
            var trimmed = response.Trim('\0', '\n', ' ');

            if (trimmed.EndsWith("OK", StringComparison.Ordinal))
                return ClamAvScanResult.Clean(trimmed);

            if (trimmed.EndsWith("FOUND", StringComparison.Ordinal))
            {
                // Format: "stream: Eicar-Test-Signature FOUND"
                var virusName = trimmed
                    .Replace("stream: ", "", StringComparison.Ordinal)
                    .Replace(" FOUND", "", StringComparison.Ordinal)
                    .Trim();
                return ClamAvScanResult.VirusFound(virusName);
            }

            return ClamAvScanResult.Error(trimmed);
        }
    }

    public sealed class ClamAvScanResult
    {
        public ClamAvScanStatus Status { get; private init; }
        public string? VirusName { get; private init; }
        public string RawResponse { get; private init; } = string.Empty;

        public static ClamAvScanResult Clean(string raw) =>
            new() { Status = ClamAvScanStatus.Clean, RawResponse = raw };

        public static ClamAvScanResult VirusFound(string virusName) =>
            new() { Status = ClamAvScanStatus.VirusDetected, VirusName = virusName, RawResponse = virusName };

        public static ClamAvScanResult Error(string raw) =>
            new() { Status = ClamAvScanStatus.Error, RawResponse = raw };
    }

    public enum ClamAvScanStatus { Clean, VirusDetected, Error }
}
