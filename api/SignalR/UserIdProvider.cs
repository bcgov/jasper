using Microsoft.AspNetCore.SignalR;
using Scv.Api.Helpers.Extensions;

namespace Scv.Api.SignalR;

public class UserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.UserId();
    }
}
