using System;
using System.Collections.Generic;
using Scv.Api.Models;

namespace Scv.Api.Models.Configuration;

public class ConstantDto : BaseDto
{
    public string Key { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
    public DateTime Ent_Dtm { get; set; }
    public string Ent_UserId { get; set; } = string.Empty;
    public DateTime Upd_Dtm { get; set; }
    public string Upd_UserId { get; set; } = string.Empty;
}
