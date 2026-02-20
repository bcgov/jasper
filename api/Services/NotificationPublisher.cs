using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Scv.Api.Hubs;
using Scv.Api.Models;

namespace Scv.Api.Services;

public interface INotificationPublisher
{
    Task NotifyUserAsync(string userId, NotificationDto notification);
    Task NotifyAllAsync(NotificationDto notification);
}

public class NotificationPublisher(IHubContext<NotificationsHub> hubContext) : INotificationPublisher
{
    private readonly IHubContext<NotificationsHub> _hubContext = hubContext;

    public Task NotifyUserAsync(string userId, NotificationDto notification)
    {
        return _hubContext.Clients.User(userId)
            .SendAsync("notificationReceived", notification);
    }

    public Task NotifyAllAsync(NotificationDto notification)
    {
        return _hubContext.Clients.All
            .SendAsync("notificationReceived", notification);
    }
}
