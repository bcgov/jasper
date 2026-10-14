using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Seeders;
using Scv.Models.Configuration;
using Xunit;

namespace tests.db.Seeders;

public class ConstantSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesOperationalExtensionsFromConfiguredValues()
    {
        await using var context = CreateContext();
        var configuration = CreateConfiguration(new()
        {
            ["DocumentConversion:DefaultEnabledExtensions:0"] = ".PDF",
            ["DocumentConversion:DefaultEnabledExtensions:1"] = ".docx",
            ["DocumentConversion:DefaultEnabledExtensions:2"] = "XLSX",
            ["DocumentConversion:DefaultEnabledExtensions:3"] = ".xlsx"
        });
        var seeder = new ConstantSeeder(Mock.Of<ILogger<ConstantSeeder>>(), configuration);

        await seeder.SeedAsync(context);

        var constant = await context.Constants.SingleAsync(value =>
            value.Key == ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions);
        Assert.Equal([".pdf", ".docx", ".xlsx"], constant.Values);
    }

    [Fact]
    public async Task SeedAsync_PreservesExistingOperationalExtensions()
    {
        await using var context = CreateContext();
        context.Constants.Add(new Constant
        {
            Key = ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions,
            Values = [".pdf", ".docx"]
        });
        await context.SaveChangesAsync();
        var configuration = CreateConfiguration(new()
        {
            ["DocumentConversion:DefaultEnabledExtensions:0"] = ".xlsx"
        });
        var seeder = new ConstantSeeder(Mock.Of<ILogger<ConstantSeeder>>(), configuration);

        await seeder.SeedAsync(context);

        var constant = await context.Constants.SingleAsync(value =>
            value.Key == ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions);
        Assert.Equal([".pdf", ".docx"], constant.Values);
    }

    private static JasperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JasperDbContext>()
            .UseInMemoryDatabase($"constant-seeder-{System.Guid.NewGuid():N}")
            .Options;
        return new TestJasperDbContext(options);
    }

    private static IConfiguration CreateConfiguration(
        System.Collections.Generic.Dictionary<string, string> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

internal sealed class TestJasperDbContext(DbContextOptions<JasperDbContext> options)
    : JasperDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Permission>();
        modelBuilder.Ignore<Role>();
        modelBuilder.Ignore<RoleAlias>();
        modelBuilder.Ignore<Group>();
        modelBuilder.Ignore<GroupAlias>();
        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<Binder>();
        modelBuilder.Ignore<DocumentCategory>();
        modelBuilder.Ignore<Case>();
        modelBuilder.Ignore<QuickLink>();
        modelBuilder.Ignore<Order>();
        modelBuilder.Ignore<EmailTemplate>();
        modelBuilder.Ignore<CourtLocation>();
        modelBuilder.Ignore<UserArtifact>();
        modelBuilder.Entity<Constant>(constant => constant.HasKey(value => value.Id));
    }
}
