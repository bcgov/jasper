using System.Collections.Generic;

namespace Scv.Api.Models.AccessControlManagement;

public class GroupDto : AccessControlManagementDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> RoleIds { get; set; } = [];
}
