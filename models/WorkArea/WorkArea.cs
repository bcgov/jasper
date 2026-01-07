namespace Scv.Models.WorkArea
{
    public class WorkArea
    {
        public required string WorkAreaDscTxt { get; set; }
        public required string RegionCd { get; set; }
        public required int WorkAreaSeqNo { get; set; }
        public bool? Active { get; set; }
    }
}