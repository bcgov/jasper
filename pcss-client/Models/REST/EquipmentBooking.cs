using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{

    public class EquipmentBooking
    {
        public string AppearanceId { get; set; }
        public string CourtDivisionCd { get; set; }
        public string ResourceId { get; set; }
        public string BookingDt { get; set; }
        public string BookingFromTm { get; set; }
        public string BookingToTm { get; set; }

        public int? LocationId { get; set; }
        public string LocationNm { get; set; }

        public string CourtRoomCd { get; set; }

        public string BookingCommentTxt { get; set; }

        public string CourtFileNumberTxt { get; set; }
        public string BookedByNm { get; set; }

        //Only for edits
        public string BookingId { get; set; }
        public string BookingCcn { get; set; }

        //For the return values form the web service
        public string ResponseMessageTxt { get; set; }
        public string ResponseCd { get; set; }
    }

    /// <summary>
    /// Class to hold the search parameters for booking equipment
    /// </summary>
    public class EquipmentSearch
    {
        public string BookingDt { get; set; }
        public string CourtDivisionCd { get; set; }
        public string AssetTypeCd { get; set; }
        public string BookingFromTm { get; set; }
        public string BookingToTm { get; set; }

        public int? PrimaryLocationId { get; set; }
        public string PrimaryCourtRoomCd { get; set; }
        public int? SecondaryLocationId { get; set; }
        public string SecondaryCourtRoomCd { get; set; }
    }

    /// <summary>
    /// For Equipment Search results
    /// </summary>
    public class Equipment
    {
        public int? LocationId { get; set; }
        public string ResourceId { get; set; }
        public string ResourceNm { get; set; }
        public string AssetTypeCd { get; set; }
        public string AssetUsageRuleCd { get; set; }
        public string CommentTxt { get; set; }
        public string PhoneNumberTxt { get; set; }
        public List<string> AvailableRooms { get; set; }
        public List<EquipmentBooking> EquipmentBookings { get; set; }
    }

    public class EquipmentSearchResults
    {
        //For the return values form the web service
        public string AppearanceId { get; set; }
        public string ResponseMessageTxt { get; set; }
        public string ResponseCd { get; set; }
        public List<Equipment> PrimaryResource { get; set; }
        public List<Equipment> SecondaryResource { get; set; }
    }


}