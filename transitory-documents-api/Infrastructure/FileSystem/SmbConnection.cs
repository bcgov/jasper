
using Scv.TdApi.Infrastructure.FileSystem;
using SMBLibrary.Client;

/// <summary>
/// Represents an isolated SMB connection that can be safely disposed
/// without affecting other operations.
/// </summary>
public sealed class SmbConnection : IDisposable
{
    private readonly ISmbClient _client;
    private readonly ILogger _logger;
    private bool _disposed;

    public ISMBFileStore FileStore { get; }

    public SmbConnection(ISmbClient client, ISMBFileStore fileStore, ILogger logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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