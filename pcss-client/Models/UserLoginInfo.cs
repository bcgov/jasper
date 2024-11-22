namespace PCSS.Models
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