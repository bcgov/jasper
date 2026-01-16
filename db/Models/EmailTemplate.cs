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
            Subject = @"Order Received for {{ case_file_number }}",
            Body = @"Dear Judge <b>{{ last_name }}</b>,<br /><br />
                     You are receiving this email because you have received an order Case File <b>{{ case_file_number }}</b>. Please login to <a href='{{ jasper_url }}'>JASPER</a> to confirm and verify.<br /><br />
                     Other pending orders for your review:<br />
                     <ul id='orders'>
                        {{ for order in orders }}
                        <li><a href='{{ order.order_url }}'>{{ order.case_file_number }}</a></li>
                        {{ end }}
                     </ul>
                     Regards,<br />
                     JASPER Support Team"
        },
    ];

    public string TemplateName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
