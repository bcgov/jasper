﻿namespace Scv.Api.Models.Calendar;

public class AdjudicatorRestriction
{
    public string PK { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string RestrictionCode { get; set; }
    public string RoomCode { get; set; }
    public bool IsCivil { get; set; }
}
