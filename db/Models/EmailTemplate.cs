using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.EMAIL_TEMPLATES)]

public class EmailTemplate : EntityBase
{
    public const string ORDER_RECEIVED = "Order Received";

    public static readonly List<EmailTemplate> ALL_EMAIL_TEMPLATES =
    [
        new EmailTemplate
        {
            TemplateName = ORDER_RECEIVED,
            Subject = @"Order Received for {{CaseFileNumber}}",
            Body = @"
Dear Judge <b>{{LastName}}</b>,
<br /><br />
You are receiving this email because you have received an order Case File <b>{{CaseFileNumber}}</b>. Please login to JASPER to confirm and verify.
<br /><br />
Regards,<br />
JASPER Support Team"
        },
    ];

    public string TemplateName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
