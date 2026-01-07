using JCCommon.Clients.FileServices;

namespace Scv.Models.Criminal.Detail
{
    /// <summary>
    /// Extends CfcBan.
    /// </summary>
    public class CriminalBan : CfcBan
    {
        public string PartId { get; set; }
    }
}