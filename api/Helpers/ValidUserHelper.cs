using System;
using System.Linq;
using Scv.Api.Helpers.Exceptions;
using Scv.Api.Models;

namespace Scv.Api.Helpers;

public class ValidUserHelper
{
    private static readonly string INACTIVE = "Inactive";
    public static void CheckIfValidUser(string responseMessage)
    {
        if (responseMessage == null) return;
        if (responseMessage.Contains("Not a valid user"))
            throw new NotAuthorizedException("No active assignment found for PartId in AgencyId");
        // ReSharper disable once StringLiteralTypo
        if (responseMessage.Contains("Agency supplied does not match Appliation Code"))
            throw new NotAuthorizedException("Agency supplied does not match Application Code");
    }

    public static bool IsPersonActive(Person person)
    {
        var latestStatus = person.Statuses.FirstOrDefault();
        return latestStatus == null || 
               latestStatus.StatusDescription != INACTIVE || 
               latestStatus.EffDate > DateTime.Now;
    }
}