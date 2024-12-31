#pragma warning disable 8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace PCSSCommon.Models
{
    public class UserLoginInfo
    {
        public string TemporaryAccessGUID { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string GUID { get; set; }
        public string AuthorizationDirectory { get; set; }
        public string AccountName { get; set; }
        public double ParticipantId { get; set; }
        public string Name { get { return string.Format("{0} {1}", GivenName, Surname); } }
    }
}

#pragma warning restore 8618