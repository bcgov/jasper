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
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        ILogger<JobFailureEmailFilterAttribute> logger = null;
        try
        {
            using var scope = JobActivator.Current.BeginScope(context);
            logger = (ILogger<JobFailureEmailFilterAttribute>)scope.Resolve(typeof(ILogger<JobFailureEmailFilterAttribute>));
            logger.LogInformation("Hangfire job {JobId} finished.", jobId);

            if (context.Exception is not null)
            {
                NotifyOfFailure(context, scope, logger);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to prepare job failure email for job {JobId}.", jobId);
        }
    }

    private static void NotifyOfFailure(PerformedContext context, JobActivatorScope scope, ILogger logger)
    {
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
        var templateData = CreateEmailData(context.BackgroundJob, context.Exception, subject);

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

    internal static object CreateEmailData(BackgroundJob job, Exception exception, string subject)
    {
        var jobType = job?.Job?.Type;
        var args = job?.Job?.Args ?? [];
        return new
        {
            subject,
            job_type = jobType?.Name ?? "UnknownJob",
            job_id = job?.Id ?? "unknown",
            args = string.Join(", ", args.Select(a => a?.ToString() ?? "null")),
            reason = jobType == typeof(SubmitOrderJob)
                ? "Order submission failed. See application logs for details."
                : exception.Message ?? "Unknown error",
            occurred_at = DateTime.UtcNow.ToString("u")
        };
    }
}
