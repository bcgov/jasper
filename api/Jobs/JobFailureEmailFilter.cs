using System;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Db.Models;

namespace Scv.Api.Jobs;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JobFailureEmailFilterAttribute : JobFilterAttribute, IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        using var scope = JobActivator.Current.BeginScope(context);
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var logger = (ILogger<JobFailureEmailFilterAttribute>)scope.Resolve(typeof(ILogger<JobFailureEmailFilterAttribute>));
        logger.LogInformation("Hangfire job {JobId} is starting.", jobId);
    }

    public void OnPerformed(PerformedContext context)
    {
        using var scope = JobActivator.Current.BeginScope(context);
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var logger = (ILogger<JobFailureEmailFilterAttribute>)scope.Resolve(typeof(ILogger<JobFailureEmailFilterAttribute>));
        logger.LogInformation("Hangfire job {JobId} finished.", jobId);

        if (context.Exception is null)
        {
            return;
        }

        var emailTemplateService = (IEmailTemplateService)scope.Resolve(typeof(IEmailTemplateService));
        var options = ((IOptions<JobsFailureEmailOptions>)scope.Resolve(typeof(IOptions<JobsFailureEmailOptions>))).Value;

        var recipients = options.Recipients?.Where(r => !string.IsNullOrWhiteSpace(r)).ToArray() ?? [];
        if (recipients.Length == 0)
        {
            return;
        }

        var subject = string.IsNullOrWhiteSpace(options.Subject)
            ? "Background job failed"
            : options.Subject;

        var jobType = context.BackgroundJob?.Job?.Type?.Name ?? "UnknownJob";
        var args = context.BackgroundJob?.Job?.Args ?? [];
        var argsText = string.Join(", ", args.Select(a => a?.ToString() ?? "null"));
        var reason = context.Exception.Message ?? "Unknown error";
        var templateData = new
        {
            subject,
            job_type = jobType,
            job_id = jobId,
            args = argsText,
            reason,
            occurred_at = DateTime.UtcNow.ToString("u")
        };

        foreach (var recipient in recipients)
        {
            try
            {
                emailTemplateService.SendEmailTemplateAsync(EmailTemplate.JOB_FAILURE, recipient, templateData)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send job failure email to {Recipient}.", recipient);
            }
        }
    }
}
