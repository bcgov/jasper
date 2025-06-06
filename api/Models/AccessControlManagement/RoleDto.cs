﻿using System.Collections.Generic;

namespace Scv.Api.Models.AccessControlManagement;

public class RoleDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> PermissionIds { get; set; } = [];
}
