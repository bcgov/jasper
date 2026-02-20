using System;

namespace Scv.Api.Models;

public record NotificationDto(
    string Type,
    string Message,
    DateTimeOffset Timestamp,
    object? Payload = null
);
