using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class AdjudicatorRestriction
    {
        public string Pk { get; set; }
        public string FileName { get; set; } //no data for this yet.

        public string JudgeName { get; set; }
        public string AppearanceReasonCode { get; set; }
        public string AppearanceReasonDescription { get; set; }
        public string RestrictionCode { get; set; }
        public bool HasIssue { get; set; }
        public string ActivityCode { get; set; }
        public string CourtRoomCode { get; set; }
        public string CourtSittingCode { get; set; }
        public int LocationId { get; set; }

        public bool IsCivil { get; set; }
        public string JustinOrCeisId { get; set; }
        public string EstimatedTimeHour { get; set; }
        public string EstimatedTimeMin { get; set; }
        public string EstimatedTimeString { get; set; }

        public AdjudicatorRestriction() { }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var t = obj as AdjudicatorRestriction;
            if (t == null) return false;
            if (Pk == t.Pk) return true;

            return false;
        }
        public override int GetHashCode()
        {
            int hash = GetType().GetHashCode();
            hash = (hash * 397) ^ Pk.GetHashCode();
            return hash;
        }
    }
}