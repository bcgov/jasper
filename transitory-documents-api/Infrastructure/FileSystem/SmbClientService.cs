using SMBLibrary;
using SMBLibrary.Client;

namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Wrapper class around SMB2Client that implements ISmbClient for testability.
    /// </summary>
    public sealed class SmbClientService : ISmbClient
    {
        private readonly SMB2Client _client;

        public SmbClientService()
        {
            _client = new SMB2Client();
        }

        public bool Connect(string serverName, SMBTransportType transport)
        {
            return _client.Connect(serverName, transport);
        }

        public NTStatus Login(string domain, string username, string password)
        {
            return _client.Login(domain, username, password);
        }

        public ISMBFileStore TreeConnect(string shareName, out NTStatus status)
        {
            return _client.TreeConnect(shareName, out status);
        }

        public void Logoff()
        {
            _client.Logoff();
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public bool IsConnected => _client.IsConnected;
    }
}
