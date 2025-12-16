using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Services;

public interface IEmailService
{
    Task<IEnumerable<Message>> GetFilteredEmailsAsync(string mailbox, string subjectPattern, string fromEmail, bool hasAttachment = true);
    Task<Dictionary<string, MemoryStream>> GetAttachmentsAsStreamsAsync(string mailbox, string messageId, string attachmentName);
}

public class EmailService(GraphServiceClient graphServiceClient) : IEmailService
{
    private readonly GraphServiceClient _graphServiceClient = graphServiceClient;

    public async Task<IEnumerable<Message>> GetFilteredEmailsAsync(string mailbox, string subjectPattern, string fromEmail, bool hasAttachment = true)
    {
        // Only emails in the last 48 hours will be queried.
        var filterCriteria = new List<string> { $"receivedDateTime ge {DateTime.UtcNow.AddHours(-48):yyyy-MM-ddTHH:mm:ssZ}" };
        var orderByCriteria = new List<string> { "receivedDateTime desc" };

        if (hasAttachment)
        {
            filterCriteria.Add("hasAttachments eq true");
            orderByCriteria.Add("hasAttachments desc");
        }

        if (!string.IsNullOrWhiteSpace(subjectPattern))
        {
            filterCriteria.Add($"startsWith(subject, '{subjectPattern.Replace("'", "''")}')");
            orderByCriteria.Add("subject");
        }

        if (!string.IsNullOrWhiteSpace(fromEmail))
        {
            filterCriteria.Add($"from/emailAddress/address eq '{fromEmail.Replace("'", "''")}'");
            orderByCriteria.Add("from/emailAddress/address");
        }

        var response = await _graphServiceClient
            .Users[mailbox]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Filter = string.Join(" and ", filterCriteria);
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "hasAttachments"];
                config.QueryParameters.Orderby = [.. orderByCriteria];
                config.QueryParameters.Top = 10;
            });

        return response.Value;
    }

    public async Task<Dictionary<string, MemoryStream>> GetAttachmentsAsStreamsAsync(
        string mailbox,
        string messageId,
        string attachmentName)
    {
        var message = await _graphServiceClient
            .Users[mailbox]
            .Messages[messageId]
            .GetAsync(config => config.QueryParameters.Expand = ["attachments"]);

        return message.Attachments?
            .OfType<FileAttachment>()
            .Where(att => (string.IsNullOrWhiteSpace(attachmentName) || att.Name.Equals(attachmentName, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(
                att => att.Name,
                att => new MemoryStream(att.ContentBytes))
            ?? [];
    }
}
