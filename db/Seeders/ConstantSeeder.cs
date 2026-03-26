using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders;

public class ConstantSeeder(ILogger<ConstantSeeder> logger) : SeederBase<JasperDbContext>(logger)
{
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

        foreach (var existing in existingConstants.Where(e => seededConstants.All(c => c.Key != e.Key)))
        {
            Logger.LogInformation("\t{Key} no longer seeded, removing it...", existing.Key);
            context.Constants.Remove(existing);
        }

        await context.SaveChangesAsync();
    }
}
