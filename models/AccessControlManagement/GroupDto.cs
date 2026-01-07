namespace Scv.Models.AccessControlManagement
{
    public class GroupDto : BaseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> RoleIds { get; set; } = [];
        public List<string> GroupAliasIds { get; set; } = [];
    }
}
