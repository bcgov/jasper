using System.Threading.Tasks;
using Scv.Models;

namespace Scv.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync<TPayload>(string userId, NotificationDto<TPayload> notification);
    Task NotifyUserWithAckAsync<TPayload>(
        string userId,
        NotificationDto<TPayload> notification);
}
