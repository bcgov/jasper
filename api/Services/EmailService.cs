using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Scv.Api.Services;

public interface IEmailService
{
    Task<IEnumerable<Message>> GetFilteredEmailsAsync(
        string mailbox,
        string subjectPattern,
        string fromEmail,
        string filenamePattern = null,
        string fileExtension = ".csv");
    Task<Dictionary<string, MemoryStream>> GetAttachmentsAsStreamsAsync(string mailbox, string messageId, string fileExtension = ".csv");
}

public class EmailService(GraphServiceClient graphServiceClient) : IEmailService
{
    private readonly GraphServiceClient _graphServiceClient = graphServiceClient;

    public async Task<IEnumerable<Message>> GetFilteredEmailsAsync(
        string mailbox,
        string subjectPattern,
        string fromEmail,
        string filenamePattern = null,
        string fileExtension = ".csv")
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
                config.QueryParameters.Top = 5;
            });

        return response.Value.Where(msg =>
            msg.Attachments?.Any(att =>
                att is FileAttachment fileAtt
                && (string.IsNullOrWhiteSpace(fileExtension) || fileAtt.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(filenamePattern) || Regex.IsMatch(fileAtt.Name, filenamePattern, RegexOptions.IgnoreCase))
            ) == true
        );
    }

    public async Task<Dictionary<string, MemoryStream>> GetAttachmentsAsStreamsAsync(
        string mailbox,
        string messageId,
        string fileExtension = ".csv")
    {
        var message = await _graphServiceClient
            .Users[mailbox]
            .Messages[messageId]
            .GetAsync(config => config.QueryParameters.Expand = ["attachments"]);

        return message.Attachments?
            .OfType<FileAttachment>()
            .Where(att => (string.IsNullOrWhiteSpace(fileExtension) || att.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase)))
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
