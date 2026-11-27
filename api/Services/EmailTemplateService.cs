using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using Scv.Core.Helpers.Extensions;
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
            var isFailureEmail = templateName is EmailTemplate.JOB_FAILURE or EmailTemplate.ORDER_SUBMISSION_FAILED;

            var templateSubject = Template.Parse(emailTemplate.Subject);
            var subject = await templateSubject.RenderAsync(data);
            if (isFailureEmail)
            {
                subject = AddDeploymentPrefix(subject, configuration["DEPLOYMENT_ENVIRONMENT"]);
            }

            var templateBody = Template.Parse(emailTemplate.Body);
            var bodyData = isFailureEmail && data is not null ? EncodeFailureEmailBodyData(data) : data;
            var body = await templateBody.RenderAsync(bodyData);

            var mailbox = configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");

            // Parse and render CC field if present
            string[] ccEmails = null;
            if (!string.IsNullOrWhiteSpace(emailTemplate.Cc))
            {
                var templateCc = Template.Parse(emailTemplate.Cc);
                var cc = await templateCc.RenderAsync(data);
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    ccEmails = cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }
            await emailService.SendEmailAsync(mailbox, recipient, subject, body, ccEmails: ccEmails);

            logger.LogInformation("Template email '{TemplateName}' sent.", templateName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send template email '{TemplateName}': {Message}", templateName, ex.Message);
        }
    }

    private static string AddDeploymentPrefix(string subject, string deploymentEnvironment)
    {
        var environment = deploymentEnvironment?.Trim().ToUpperInvariant() switch
        {
            "DEV" or "LZ-DEV" => "DEV",
            "TEST" or "LZ-TEST" => "TEST",
            "PROD" or "LZ-PROD" => "PROD",
            _ => "UNKNOWN"
        };

        // Stored templates and configured subjects can already supply an environment prefix.
        var subjectText = Regex.Replace(subject.TrimStart(), @"^(?:\[(?:DEV|TEST|PROD|UNKNOWN)\]\s*)+", "",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        return $"[{environment}] {subjectText}";
    }

    private static ScriptObject EncodeFailureEmailBodyData(object data)
    {
        var bodyData = new ScriptObject();
        bodyData.Import(data);
        foreach (var key in bodyData.Keys.ToArray())
        {
            if (bodyData[key] is string value)
            {
                bodyData[key] = WebUtility.HtmlEncode(value);
            }
        }

        return bodyData;
    }
}
