using Hangfire.Dashboard;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Authorization;

public class HangFireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;

        return user != null
            && user.Identity.IsAuthenticated
            && user.HasRoles([Role.ADMIN]);
    }
}
