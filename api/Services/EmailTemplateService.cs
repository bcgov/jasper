using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scriban;
using Scv.Api.Helpers;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IEmailTemplateService
{
    Task SendEmailTemplateAsync(string templateName, string recipient, object data);
}

public class EmailTemplateService(
    IRepositoryBase<EmailTemplate> emailTemplateRepo,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    public async Task SendEmailTemplateAsync(string templateName, string recipient, object data)
    {
        try
        {
            var emailTemplateResult = await emailTemplateRepo.FindAsync(e => e.TemplateName == templateName);
            if (emailTemplateResult == null || !emailTemplateResult.Any())
            {
                logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                return;
            }

            var emailTemplate = emailTemplateResult.First();

            var templateSubject = Template.Parse(emailTemplate.Subject);
            var subject = await templateSubject.RenderAsync(data);

            var templateBody = Template.Parse(emailTemplate.Body);
            var body = await templateBody.RenderAsync(data);

            var mailbox = configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");

            await emailService.SendEmailAsync(mailbox, recipient, subject, body);

            logger.LogInformation("Template email '{TemplateName}' sent.", templateName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send template email '{TemplateName}': {Message}", templateName, ex.Message);
        }
    }
}
