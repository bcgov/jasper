using System.Collections.Generic;

namespace Scv.Api.Models.UserManagement;

public class RoleCreateDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> PermissionIds { get; set; } = [];
}
