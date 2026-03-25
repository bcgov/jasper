using System.Collections.Generic;
using Scv.Api.Models;

namespace Scv.Api.Models.Configuration;

public class ConstantDto : AuditableDto
{
    public string Key { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
}
