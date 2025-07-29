#pragma warning disable 8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace PCSSCommon.Models;
public class PersonSearchItem
{
    public int PersonId { get; set; }

    public int UserId { get; set; }
    public double? ParticipantId { get; set; }

    public int HomeLocationId { get; set; }
    public string HomeLocationName { get; set; }
    public string RegionCode { get; set; }
    public string RegionDescription { get; set; }
    public int WorkAreaSeqNo { get; set; }
    public string WorkAreaDescription { get; set; }

    public string RotaInitials { get; set; }

    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Initials { get; set; }
    public string FullName { get; set; }

    public string JudiciaryTypeCode { get; set; }
    public string JudiciaryTypeDescription { get; set; }
    public string PositionCode { get; set; }
    public string PositionDescription { get; set; }
    public string StatusCode { get; set; }
    public string StatusDescription { get; set; }

    public string InactiveReasonCode { get; set; }
    public string InactiveReasonDescription { get; set; }

    public bool IsNonStatus { get; set; }

    public string ScheduleGeneratedDate { get; set; }
    public string SchedulePublishedDate { get; set; }
}

#pragma warning restore 8618