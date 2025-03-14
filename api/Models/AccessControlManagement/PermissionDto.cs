namespace Scv.Api.Models.AccessControlManagement;

public class PermissionDto : AccessControlManagementDto
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool? IsActive { get; set; }
}
