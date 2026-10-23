using Microsoft.AspNetCore.Http;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;

namespace Scv.Api.Infrastructure;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId => httpContextAccessor.HttpContext?.User?.UserId();
}
