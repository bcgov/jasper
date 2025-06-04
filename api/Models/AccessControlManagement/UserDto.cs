using System;
using System.Collections.Generic;

namespace Scv.Api.Models.AccessControlManagement;

public class UserDto : BaseDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public Guid? ADId { get; set; }
    public string ADUsername { get; set; }
    public List<string> GroupIds { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Roles { get; set; } = [];
}
