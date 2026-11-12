using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders;

public class AppDocumentSeeder(ILogger<AppDocumentSeeder> logger) : SeederBase<JasperDbContext>(logger)
{
    public override int Order => 9;

    protected async override Task ExecuteAsync(JasperDbContext context)
    {
        var allDefaultAppDocuments = AppDocument.ALL_APP_DOCUMENTS;

        this.Logger.LogInformation("\tUpserting app documents...");
        foreach (var appDoc in allDefaultAppDocuments)
        {
            var ad = await context.AppDocuments.FirstOrDefaultAsync(et => et.Key == appDoc.Key);
            if (ad == null)
            {
                await context.AppDocuments.AddAsync(appDoc);
            }
            else
            {
                this.Logger.LogInformation("{Key} app document already exists. Skipping...", appDoc.Key);
            }
        }

        await context.SaveChangesAsync();
    }
}
