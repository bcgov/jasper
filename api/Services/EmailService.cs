using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Scv.Api.Services;

public interface IEmailService
{
    Task<IEnumerable<Message>> GetFilteredEmailsAsync(
        string mailbox,
        string subjectPattern,
        string fromEmail,
        string csvFilenamePattern = null);
    Task<Dictionary<string, MemoryStream>> GetCsvAttachmentsAsStreamsAsync(string mailbox, string messageId);
}

public class EmailService(ILogger<EmailService> logger, GraphServiceClient graphServiceClient) : IEmailService
{
    private readonly ILogger<EmailService> _logger = logger;
    private readonly GraphServiceClient _graphServiceClient = graphServiceClient;

    public async Task<IEnumerable<Message>> GetFilteredEmailsAsync(
        string mailbox,
        string subjectPattern,
        string fromEmail,
        string csvFilenamePattern = null)
    {
        var filters = new List<string> { "hasAttachments eq true" };

        if (!string.IsNullOrEmpty(subjectPattern))
        {
            filters.Add($"contains(subject, '{EscapeODataString(subjectPattern)}')");
        }

        if (!string.IsNullOrEmpty(fromEmail))
        {
            filters.Add($"from/emailAddress/address eq '{EscapeODataString(fromEmail)}'");
        }

        var filterQuery = string.Join(" and ", filters);

        var response = await _graphServiceClient
            .Users[mailbox]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Filter = filterQuery;
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "hasAttachments"];
                config.QueryParameters.Expand = ["attachments"];
                config.QueryParameters.Orderby = ["receivedDateTime desc"];
                config.QueryParameters.Top = 100;
            });

        return response.Value.Where(msg =>
            msg.Attachments?.Any(att =>
                att is FileAttachment fileAtt &&
                fileAtt.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(csvFilenamePattern) ||
                 Regex.IsMatch(fileAtt.Name, csvFilenamePattern, RegexOptions.IgnoreCase))
            ) == true
        );
    }

    public async Task<Dictionary<string, MemoryStream>> GetCsvAttachmentsAsStreamsAsync(
        string mailbox,
        string messageId)
    {
        var message = await _graphServiceClient
            .Users[mailbox]
            .Messages[messageId]
            .GetAsync(config => config.QueryParameters.Expand = ["attachments"]);

        return message.Attachments?
            .OfType<FileAttachment>()
            .Where(att => att.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                att => att.Name,
                att => new MemoryStream(att.ContentBytes))
            ?? [];
    }

    private static string EscapeODataString(string value)
    {
        return value.Replace("'", "''");
    }
}
