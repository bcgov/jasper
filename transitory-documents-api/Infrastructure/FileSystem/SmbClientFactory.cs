namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Default implementation of ISmbClientFactory that creates SmbClientWrapper instances.
    /// </summary>
    public sealed class SmbClientFactory : ISmbClientFactory
    {
        public ISmbClient CreateClient()
        {
            return new SmbClientService();
        }
    }
}
