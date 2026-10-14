using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Models.Configuration;

namespace Scv.Db.Seeders;

public class ConstantSeeder(
    ILogger<ConstantSeeder> logger,
    IConfiguration configuration) : SeederBase<JasperDbContext>(logger)
{
    private static readonly HashSet<string> SeedManagedKeys =
        ["ReleaseNotesUrl"];

    public override int Order => 8;

    protected override async Task ExecuteAsync(JasperDbContext context)
    {
        var seededConstants = new List<Constant>
        {
            new()
            {
                Key = "ReleaseNotesUrl",
                Values = ["https://provincialcourt.sharepoint.com/sites/JASPER"]
            }
        };

        Logger.LogInformation("\tUpdating constants...");

        var existingConstants = await context.Constants.ToListAsync();

        foreach (var constant in seededConstants)
        {
            var existing = await context.Constants.AsQueryable().FirstOrDefaultAsync(c => c.Key == constant.Key);
            if (existing == null)
            {
                Logger.LogInformation("\t{Key} does not exist, adding it...", constant.Key);
                await context.Constants.AddAsync(constant);
            }
            else
            {
                Logger.LogInformation("\tUpdating values for {Key}...", constant.Key);
                existing.Values = constant.Values;
            }
        }

        if (existingConstants.All(c => c.Key != ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions))
        {
            var defaultExtensions = configuration
                .GetSection("DocumentConversion:DefaultEnabledExtensions")
                .GetChildren()
                .Select(section => NormalizeExtension(section.Value))
                .Where(value => value is not null)
                .OfType<string>()
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (defaultExtensions.Length == 0)
            {
                Logger.LogError(
                    "\t{Key} does not exist and no defaults are configured; the operational constant was not created.",
                    ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions);
            }
            else
            {
                Logger.LogInformation(
                    "\t{Key} does not exist, adding operational defaults...",
                    ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions);
                await context.Constants.AddAsync(new Constant
                {
                    Key = ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions,
                    Values = [.. defaultExtensions]
                });
            }
        }

        foreach (var existing in existingConstants.Where(e =>
            SeedManagedKeys.Contains(e.Key) && seededConstants.All(c => c.Key != e.Key)))
        {
            Logger.LogInformation("\t{Key} no longer seeded, removing it...", existing.Key);
            context.Constants.Remove(existing);
        }

        await context.SaveChangesAsync();
    }

    private static string NormalizeExtension(string extension)
    {
        var normalized = extension?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized[0] != '.') normalized = $".{normalized}";
        return normalized.Length is > 1 and <= 16 && normalized.Skip(1).All(char.IsLetterOrDigit)
            ? normalized
            : null;
    }
}
