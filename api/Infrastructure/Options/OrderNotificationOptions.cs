using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.Configuration;
using Scv.Models.Order;

namespace Scv.Api.Infrastructure.Options;

/// <summary>
/// Filters that decide whether an order triggers an email notification.
/// Each setting is a comma-separated list; an empty or missing setting means "any".
/// </summary>
public sealed class OrderNotificationOptions : IValidatableObject
{
    public const string PriorityTypesKey = "NOTIFY_PRIORITY_TYPES";
    public const string OrderTypesKey = "NOTIFY_ORDER_TYPES";
    public const string CourtClassesKey = "NOTIFY_COURT_CLASSES";

    private static readonly string[] ValidOrderTypes = [CourtListTypeDescriptor.ORDER_DESCRIPTION, CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION];
    private static readonly CourtClassCd[] ValidCourtClasses = [CourtClassCd.F, CourtClassCd.C];

    [ConfigurationKeyName(PriorityTypesKey)]
    public string PriorityTypes { get; set; }

    [ConfigurationKeyName(OrderTypesKey)]
    public string OrderTypes { get; set; }

    [ConfigurationKeyName(CourtClassesKey)]
    public string CourtClasses { get; set; }

    public IReadOnlyList<string> GetPriorityTypes() => Split(PriorityTypes);

    public IReadOnlyList<string> GetOrderTypes() => Split(OrderTypes);

    public IReadOnlyList<string> GetCourtClasses() => Split(CourtClasses);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var value in GetOrderTypes().Where(v => !ValidOrderTypes.Contains(v, StringComparer.OrdinalIgnoreCase)))
        {
            yield return new ValidationResult($"Unrecognized {OrderTypesKey} value: {value}", [nameof(OrderTypes)]);
        }

        foreach (var value in GetCourtClasses().Where(v => !ValidCourtClasses.Contains(Enum.TryParse<CourtClassCd>(v, true, out var c) ? c : default)))
        {
            yield return new ValidationResult($"Unrecognized {CourtClassesKey} value: {value}", [nameof(CourtClasses)]);
        }

        foreach (var value in GetPriorityTypes().Where(v => !PriorityTypeDescriptor.PRIORITY_ORDERS.Contains(v, StringComparer.OrdinalIgnoreCase)))
        {
            yield return new ValidationResult($"Unrecognized {PriorityTypesKey} value: {value}", [nameof(PriorityTypes)]);
        }
    }

    private static List<string> Split(string value) =>
        [.. (value ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
