using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders;

public class EmailTemplateSeeder(ILogger<EmailTemplateSeeder> logger) : SeederBase<JasperDbContext>(logger)
{
    public override int Order => 6;

    protected override async Task ExecuteAsync(JasperDbContext context)
    {
        var allDefaultTemplates = EmailTemplate.ALL_EMAIL_TEMPLATES;

        this.Logger.LogInformation("\tAdding email templates...");
        foreach (var template in allDefaultTemplates)
        {
            var t = await context.EmailTemplates.FirstOrDefaultAsync(et => et.TemplateName == template.TemplateName);
            if (t == null)
            {
                await context.EmailTemplates.AddAsync(template);
            }
            else
            {
                this.Logger.LogInformation("{TemplateName} email template already exists. Skipping...", template.TemplateName);
            }
        }

        await context.SaveChangesAsync();
    }
}
