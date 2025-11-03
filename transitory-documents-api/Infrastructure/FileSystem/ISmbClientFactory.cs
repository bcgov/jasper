namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Factory for creating SMB client instances.
    /// </summary>
    public interface ISmbClientFactory
    {
        ISmbClient CreateClient();
    }
}