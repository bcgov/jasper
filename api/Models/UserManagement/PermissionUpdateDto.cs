namespace Scv.Api.Models.UserManagement;

public class PermissionUpdateDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool? IsActive { get; set; }
}
