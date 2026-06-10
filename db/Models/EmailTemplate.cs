using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.EMAIL_TEMPLATES)]

public class EmailTemplate : EntityBase
{
    public const string ORDER_RECEIVED = "Order Received";
    public const string JOB_FAILURE = "Job Failure";
    public const string ORDER_REMINDER = "Order Reminder";
    public const string ORDER_REASSIGNMENT = "Order Reassignment";

    public static readonly List<EmailTemplate> ALL_EMAIL_TEMPLATES =
    [
        new EmailTemplate
        {
            TemplateName = ORDER_RECEIVED,
            Subject = @"{{ if is_priority }}Priority Order ({{ priority_type_desc }}) Received{{ else }}Order Received{{ end }} for {{ location_shortname }} {{ case_file_number }}",
            Body = @"Dear Judge <b>{{ last_name }}</b>,<br /><br />
                     You have received an order for {{ location_name }} {{ case_file_number }} on {{ date_received }}. <br /><br />
                     Regards,<br />
                     JASPER Support Team<br /><br />
                     <i>Please note: This is an automated message. Please do not reply directly to this email.</i>"
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
        new EmailTemplate
        {
            TemplateName = ORDER_REMINDER,
            Subject = @"{{ if is_priority }}Priority Order ({{ priority_type_desc }}) {{ else }}Order{{ end }} {{ location_name }} {{ case_file_number }} is still waiting to be processed.",
            Body = @"Dear Judge {{ judge_name }},<br /><br />
                     The following order was received on {{ date_received }} and has not yet been completed. Please review and complete {{ location_name }} {{ case_file_number }} as soon as possible.<br /><br />
                     Regards,<br />
                     JASPER Support Team<br /><br />
                     <i>Please note: This is an automated message. Please do not reply directly to this email.</i>"
        },
        new EmailTemplate
        {
            TemplateName = ORDER_REASSIGNMENT,
            Subject = @"{{ if is_priority }}Priority Order ({{ priority_type_desc }}) {{ else }}Order{{ end }} {{ location_name }} {{ case_file_number }} is overdue and has been assigned to you.",
            Body = @"Dear Judge {{ judge_name }},<br /><br />
                     The following order was received on {{ date_received }} and has not yet been completed. Please review and complete {{ location_name }} {{ case_file_number }} as soon as possible.<br /><br />
                     Regards,<br />
                     JASPER Support Team<br /><br />
                     <i>Please note: This is an automated message. Please do not reply directly to this email.</i>",
            Cc = "{{ support_account }}"
        }
    ];

    public string TemplateName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Cc { get; set; }
}
