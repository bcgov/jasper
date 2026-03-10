using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync<TPayload>(string userId, NotificationDto<TPayload> notification);
    Task NotifyUserWithAckAsync<TPayload>(
        string userId,
        NotificationDto<TPayload> notification);
}
