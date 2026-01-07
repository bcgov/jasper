using Microsoft.EntityFrameworkCore;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.Authorization
{
    public class VcCivilFileAccessHandler
    {
        private ScvDbContext Db { get; }
        public VcCivilFileAccessHandler(ScvDbContext db)
        {
            Db = db;
        }
        public async Task<bool> HasCivilFileAccess(ClaimsPrincipal user, string civilFileId)
        {
            var userId = user.ExternalUserId();
            var now = DateTimeOffset.UtcNow;
            var fileAccess = await Db.RequestFileAccess
                .AnyAsync(r => r.UserId == userId && r.Expires > now && r.FileId == civilFileId);
            return fileAccess;
        }
    }
}
