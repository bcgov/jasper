
namespace Scv.Models.Civil.Appearances
{
    public class CivilAppearanceResponse : JCCommon.Clients.FileServices.CivilFileAppearancesResponse
    {
        /// <summary>
        /// Extended object.
        /// </summary>
        public new System.Collections.Generic.ICollection<CivilAppearance> ApprDetail { get; set; }
    }
}
