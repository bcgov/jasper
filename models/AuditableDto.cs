using System;

namespace Scv.Models;

public class AuditableDto : BaseDto
{
    public DateTime Ent_Dtm { get; set; }
    public string Ent_UserId { get; set; } = string.Empty;
    public DateTime Upd_Dtm { get; set; }
    public string Upd_UserId { get; set; } = string.Empty;
}