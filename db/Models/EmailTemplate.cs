using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.EMAIL_TEMPLATES)]

public class EmailTemplate : EntityBase
{
    public const string ORDER_RECEIVED = "Order Received";
    public const string JOB_FAILURE = "Job Failure";

    public static readonly List<EmailTemplate> ALL_EMAIL_TEMPLATES =
    [
        new EmailTemplate
        {
            TemplateName = ORDER_RECEIVED,
            Subject = @" <<Priority>>> Order Received for {{ location_shortname }} {{ case_file_number }}",
            Body = @"Dear Judge <b>{{ last_name }}</b>,<br /><br />
                     You have received an order for <a href='{{ url }}'>{{ location_name }} {{ case_file_number }}</a> on {{ date_received }}. <br /><br />
                     Other pending orders for your review:<br />
                     <ul id='orders'>
                        {{ for order in orders }}
                        <li><a href='{{ order.order_url }}'>{{ order.case_file_number }}</a></li>
                        {{ end }}
                     </ul>
                     Regards,<br />
                     JASPER Support Team"
        },
        new EmailTemplate
        {
            TemplateName = JOB_FAILURE,
            Subject = @"{{ subject }}",
            Body = @"<p>Background job failed.</p>
                     <p>Job: {{ job_type }}</p>
                     <p>Job Id: {{ job_id }}</p>
                     <p>Arguments: {{ args }}</p>
                     <p>Reason: {{ reason }}</p>
                     <p>Occurred At (UTC): {{ occurred_at }}</p>"
        },
    ];

    public string TemplateName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
