using System;
using System.Net.Http.Headers;
using System.Text;

namespace JCCommon.Framework
{
    // From here; https://github.com/IdentityModel/Thinktecture.IdentityModel.Web/blob/master/Thinktecture.IdentityModel.Web/BasicAuthenticationHeaderValue.cs
    public class BasicAuthenticationHeaderValue(string userName, string password) : AuthenticationHeaderValue("Basic", EncodeCredential(userName, password))
    {
        private static string EncodeCredential(string userName, string password)
        {
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string credential = $"{userName}:{password}";
            return Convert.ToBase64String(encoding.GetBytes(credential));
        }
    }
}
