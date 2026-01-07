namespace Scv.Models.Criminal.Detail
{
    /// <summary>
    /// Extends  JCCommon.Clients.FileServices.CriminalWitness
    /// </summary>
    public class CriminalWitness : JCCommon.Clients.FileServices.CriminalWitness
    {
        public string? FullName => GivenNm != null && LastNm != null
            ? $"{GivenNm.Trim()} {LastNm.Trim()}"
            : null;

        public string WitnessTypeDsc { get; set; }
        public string AgencyDsc { get; set; }
        public string AgencyCd { get; set; }
    }
}