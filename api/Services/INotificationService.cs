using System.Threading.Tasks;
using Scv.Api.Models;

namespace Scv.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync<TPayload>(string userId, NotificationDto<TPayload> notification);
    Task NotifyUserWithAckAsync<TPayload>(
        string userId,
        NotificationDto<TPayload> notification);
}
