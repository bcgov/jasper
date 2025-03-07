using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Scv.Api.Models.UserManagement;

public class PermissionUpdateDto
{
    [Required(ErrorMessage = "Description is required.")]
    public required string Description { get; set; }
    [Required(ErrorMessage = "IsActive is required.")]
    public required bool? IsActive { get; set; }
}
