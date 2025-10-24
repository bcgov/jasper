namespace Scv.Models.Criminal.AppearanceDetail
{
    /// <summary>
    /// Extends <see cref="JCCommon.Clients.FileServices.CriminalAppearanceMethod"/>
    /// </summary>
    public class CriminalAppearanceMethod : JCCommon.Clients.FileServices.CriminalAppearanceMethod
    {
        public string RoleTypeDsc { get; set; }
        public string AppearanceMethodDesc { get; set; }
    }
}