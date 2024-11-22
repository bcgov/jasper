namespace PCSS.Models.REST
{
    public class CourtActivity
    {
        public virtual int? CourtActivityId { get; set; }
        public virtual int? CourtActivitySlotId { get; set; }
        public virtual string CourtActivityDt { get; set; }
        public virtual int LocationId { get; set; }
        public virtual string CourtRoomCd { get; set; }
        public virtual string ActivityCd { get; set; }
        public virtual string ActivityDsc { get; set; }
        public virtual string CourtSittingCd { get; set; }
        public virtual string StartTm { get; set; }

        public string ActivityClassCd { get; set; }

        public string ActivityClassDsc { get; set; }
    }
}
