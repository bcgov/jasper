namespace Scv.Api.Models.UserManagement;

public class PermissionDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}
