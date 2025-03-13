using System.Collections.Generic;

namespace Scv.Api.Models.UserManagement;

public class RoleDto : UserManagementDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> PermissionIds { get; set; } = [];
}
