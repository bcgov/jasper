using SMBLibrary;
using SMBLibrary.Client;

namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Interface wrapper around SMB2Client to enable testability.
    /// </summary>
    public interface ISmbClient
    {
        /// <summary>
        /// Connect to an SMB server.
        /// </summary>
        bool Connect(string serverName, SMBTransportType transport);

        /// <summary>
        /// Login to the SMB server.
        /// </summary>
        NTStatus Login(string domain, string username, string password);

        /// <summary>
        /// Connect to a share on the SMB server.
        /// </summary>
        ISMBFileStore TreeConnect(string shareName, out NTStatus status);

        /// <summary>
        /// Logoff from the SMB server.
        /// </summary>
        void Logoff();

        /// <summary>
        /// Disconnect from the SMB server.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets whether the client is currently connected.
        /// </summary>
        bool IsConnected { get; }
    }
}
