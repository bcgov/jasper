namespace Scv.Models.JCUserService
{
    /// <summary>
    /// Used to shorten the number of parameters for UserInfo.
    /// </summary>
    public class UserInfoRequest
    {
        public string DomainName { get; set; }
        public string DomainUserGuid { get; set; }
        public string DomainUserId { get; set; }
        public string LoginDtm { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string TemporaryAccessGuid { get; set; }
    }
}
