using System;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JobFailureEmailFilterAttribute : JobFilterAttribute, IServerFilter
{
    private static T Resolve<T>(PerformContext context)
    {
        using var scope = JobActivator.Current.BeginScope(context);
        return (T)scope.Resolve(typeof(T));
    }

    public void OnPerforming(PerformingContext context)
    {
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var logger = Resolve<ILogger<JobFailureEmailFilterAttribute>>(context);
        logger.LogInformation("Hangfire job {JobId} is starting.", jobId);
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var logger = Resolve<ILogger<JobFailureEmailFilterAttribute>>(context);
        logger.LogInformation("Hangfire job {JobId} finished.", jobId);

        if (context.Exception is null)
        {
            return;
        }

        var emailService = Resolve<IEmailService>(context);
        var configuration = Resolve<IConfiguration>(context);
        var options = Resolve<IOptions<JobsFailureEmailOptions>>(context).Value;

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
        var body = $"<p>Background job failed.</p><p>Job: {jobType}</p><p>Arguments: {argsText}</p><p>Reason: {reason}</p>";
        var mailbox = configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");

        foreach (var recipient in recipients)
        {
            try
            {
                emailService.SendEmailAsync(mailbox, recipient, subject, body).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send job failure email to {Recipient}.", recipient);
            }
        }
    }
}
