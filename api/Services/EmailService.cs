using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Scv.Api.Services;

public interface IEmailService
{
    Task<IEnumerable<Message>> GetFilteredEmailsAsync(string mailbox, string subjectPattern, string fromEmail);
    Task<Dictionary<string, MemoryStream>> GetAttachmentsAsStreamsAsync(string mailbox, string messageId, string attachmentName);
}

public class EmailService(GraphServiceClient graphServiceClient) : IEmailService
{
    private readonly GraphServiceClient _graphServiceClient = graphServiceClient;

    public async Task<IEnumerable<Message>> GetFilteredEmailsAsync(string mailbox, string subjectPattern, string fromEmail)
    {
        var response = await _graphServiceClient
            .Users[mailbox]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "hasAttachments"];
                config.QueryParameters.Orderby = ["receivedDateTime desc"];
                config.QueryParameters.Top = 50;
            });

        return response.Value
            .Where(msg =>
                (string.IsNullOrWhiteSpace(subjectPattern) || msg.Subject.Equals(subjectPattern, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(fromEmail) || msg.From.EmailAddress.Address.Equals(fromEmail, StringComparison.OrdinalIgnoreCase))
                && msg.HasAttachments.GetValueOrDefault()
            );
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
