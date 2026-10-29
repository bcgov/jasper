using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Scv.Core.Infrastructure;
using Scv.Db.Contexts;
using Scv.Db.Interceptors;
using Scv.Db.Models;
using Xunit;

namespace tests.db.Interceptors;

public class AuditInterceptorTests
{
    private static JasperDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<JasperDbContext>()
            .UseMongoDB("mongodb://localhost:27017", "audit-tests")
            .Options);

    private static AuditInterceptor CreateInterceptor(string userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);

        return new AuditInterceptor(currentUser.Object);
    }

    private static async Task InterceptAsync(AuditInterceptor interceptor, JasperDbContext context)
    {
        var eventData = new DbContextEventData(null, null, context);
        await interceptor.SavingChangesAsync(eventData, default);
    }

    [Fact]
    public async Task SavingChanges_SetsEntAndUpdFields_WhenEntityAdded()
    {
        using var context = CreateContext();
        var constant = new Constant { Key = "k" };
        context.Constants.Add(constant);

        await InterceptAsync(CreateInterceptor("user-1"), context);

        Assert.Equal("user-1", constant.Ent_UserId);
        Assert.Equal("user-1", constant.Upd_UserId);
        Assert.NotEqual(default, constant.Ent_Dtm);
        Assert.NotEqual(default, constant.Upd_Dtm);
    }

    [Fact]
    public async Task SavingChanges_OnlyUpdatesUpdFields_WhenEntityModified()
    {
        using var context = CreateContext();
        var constant = new Constant { Key = "k", Ent_UserId = "creator" };
        context.Constants.Attach(constant);
        context.Entry(constant).State = EntityState.Modified;

        await InterceptAsync(CreateInterceptor("user-2"), context);

        Assert.Equal("creator", constant.Ent_UserId);
        Assert.Equal("user-2", constant.Upd_UserId);
        Assert.Equal(default, constant.Ent_Dtm);
        Assert.NotEqual(default, constant.Upd_Dtm);
    }

    [Fact]
    public async Task SavingChanges_UsesSystemUser_WhenNoHttpContext()
    {
        using var context = CreateContext();
        var constant = new Constant { Key = "k" };
        context.Constants.Add(constant);

        await InterceptAsync(CreateInterceptor(null), context);

        Assert.Equal(AuditInterceptor.SystemUserId, constant.Ent_UserId);
        Assert.Equal(AuditInterceptor.SystemUserId, constant.Upd_UserId);
    }
}
