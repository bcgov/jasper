using Scv.Core.Helpers.Exceptions;

namespace Scv.Api.Helpers
{
    public class ValidUserHelper
    {
        public static void CheckIfValidUser(string responseMessage)
        {
            if (responseMessage == null) return;
            if (responseMessage.Contains("Not a valid user"))
                throw new NotAuthorizedException("No active assignment found for PartId in AgencyId");
            // ReSharper disable once StringLiteralTypo
            if (responseMessage.Contains("Agency supplied does not match Appliation Code"))
                throw new NotAuthorizedException("Agency supplied does not match Application Code");
        }
    }
}
