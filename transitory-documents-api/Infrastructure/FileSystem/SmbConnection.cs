using SMBLibrary.Client;

namespace Scv.TdApi.Infrastructure.FileSystem
{

    /// <summary>
    /// Represents an isolated SMB connection that can be safely disposed
    /// without affecting other operations.
    /// </summary>
    public sealed class SmbConnection(ISmbClient client, ISMBFileStore fileStore, ILogger logger) : IDisposable
    {
        private readonly ISmbClient _client = client ?? throw new ArgumentNullException(nameof(client));
        private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private bool _disposed;

        public ISMBFileStore FileStore { get; } = fileStore ?? throw new ArgumentNullException(nameof(fileStore));

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                FileStore?.Disconnect();
                _client?.Logoff();
                _client?.Disconnect();
                _logger.LogDebug("SMB connection disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing SMB connection");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
