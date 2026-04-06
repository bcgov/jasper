namespace Scv.Models.AccessControlManagement
{
    public class GroupAliasDto : BaseDto
    {
        public string Name { get; set; }

        public GroupDto Group { get; set; }
    }
}
