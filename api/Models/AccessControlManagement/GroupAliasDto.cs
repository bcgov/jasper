using System.Collections.Generic;

namespace Scv.Api.Models.AccessControlManagement;

public class GroupAliasDto : BaseDto
{
    public string Name { get; set; }

    public GroupDto Group { get; set; }
}
