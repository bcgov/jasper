using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders;

public class QuickLinkSeeder(ILogger<QuickLinkSeeder> logger, IConfiguration config) : SeederBase<JasperDbContext>(logger)
{
    private readonly IConfiguration _config = config;

    public override int Order => 5;

    protected async override Task ExecuteAsync(JasperDbContext context)
    {
        var defaultQuickLinksJson = _config["DEFAULT_QUICK_LINKS"];
        var quickLinksObj = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(defaultQuickLinksJson);
        var quickLinks = new List<QuickLink>();

        foreach (var quickLinkObj in quickLinksObj)
        {
            var newQuickLink = new QuickLink
            {
                Name = quickLinkObj["Name"],
                ParentName = quickLinkObj["ParentName"],
                IsMenu = bool.Parse(quickLinkObj["IsMenu"]),
                URL = quickLinkObj["URL"],
                Order = int.Parse(quickLinkObj["Order"]),
                JudgeId = quickLinkObj["JudgeId"]
            };
            quickLinks.Add(newQuickLink);
        }

        Logger.LogInformation("\tUpdating quick links...");

        var existingQuickLinks = await context.QuickLinks.ToListAsync();

        // Add or update quick links from config
        foreach (var link in quickLinks)
        {
            var ql = await context.QuickLinks.AsQueryable().FirstOrDefaultAsync(
                ql => ql.Name == link.Name
                && ql.ParentName == link.ParentName);
            if (ql == null)
            {
                Logger.LogInformation("\t{Name} does not exist, adding it...", link.Name);
                await context.QuickLinks.AddAsync(link);
            }
            else
            {
                Logger.LogInformation("\tUpdating fields for {Name}...", link.Name);
                ql.URL = link.URL;
                ql.Order = link.Order;
                ql.ParentName = link.ParentName;
                ql.IsMenu = link.IsMenu;
                ql.JudgeId = link.JudgeId;
            }
        }

        // Remove default quick links that exist in DB but not in config
        foreach (var existingLink in existingQuickLinks.Where(ql => ql.JudgeId == null))
        {
            var stillExists = quickLinks.Exists(ql =>
                ql.Name == existingLink.Name &&
                ql.ParentName == existingLink.ParentName);

            if (!stillExists)
            {
                Logger.LogInformation("\t{Name} no longer in config, removing it...", existingLink.Name);
                context.QuickLinks.Remove(existingLink);
            }
        }

        await context.SaveChangesAsync();
    }
}