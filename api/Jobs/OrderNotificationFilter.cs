using System;
using System.Collections.Generic;
using System.Linq;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.Configuration;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

/// <summary>
/// Determine whether an order should trigger an email notification based on three optional filters.
/// Each filter is a comma-separated setting; an empty or missing setting means "any".
/// An order must match every configured filter.
/// <list type="bullet">
/// <item>NOTIFY_PRIORITY_TYPES: priority markers, e.g. "PRO,CRTD,OTHR".</item>
/// <item>NOTIFY_ORDER_TYPES: "Order" and/or "Desk Order".</item>
/// <item>NOTIFY_COURT_CLASSES: "F" and/or "C".</item>
/// </list>
/// </summary>
public class OrderNotificationFilter(IConfiguration configuration)
{
    public const string PriorityTypesKey = "NOTIFY_PRIORITY_TYPES";
    public const string OrderTypesKey = "NOTIFY_ORDER_TYPES";
    public const string CourtClassesKey = "NOTIFY_COURT_CLASSES";

    private static readonly string[] ValidOrderTypes = [CourtListTypeDescriptor.ORDER_DESCRIPTION, CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION];
    private static readonly CourtClassCd[] ValidCourtClasses = [CourtClassCd.F, CourtClassCd.C];

    private readonly IConfiguration _configuration = configuration;

    public IReadOnlyList<string> GetUnrecognizedValues() =>
    [
        .. GetValues(OrderTypesKey)
            .Where(v => !ValidOrderTypes.Contains(v, StringComparer.OrdinalIgnoreCase))
            .Select(v => $"{OrderTypesKey}={v}"),
        .. GetValues(CourtClassesKey)
            .Where(v => !ValidCourtClasses.Contains(Enum.TryParse<CourtClassCd>(v, true, out var c) ? c : default))
            .Select(v => $"{CourtClassesKey}={v}"),
        .. GetValues(PriorityTypesKey)
            .Where(v => !PriorityTypeDescriptor.PRIORITY_ORDERS.Contains(v, StringComparer.OrdinalIgnoreCase))
            .Select(v => $"{PriorityTypesKey}={v}")
    ];

    /// <summary>
    /// Returns null when the order matches all filters; otherwise the name of the first filter that rejected it.
    /// </summary>
    public string GetRejectedFilter(OrderRequestDto orderRequest)
    {
        var priorityTypes = GetValues(PriorityTypesKey);
        if (priorityTypes.Count > 0
            && !priorityTypes.Contains(orderRequest?.Referral?.PriorityType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return PriorityTypesKey;
        }

        var orderTypes = GetValues(OrderTypesKey);
        if (orderTypes.Count > 0 && !orderTypes.Contains(ResolveOrderType(orderRequest), StringComparer.OrdinalIgnoreCase))
        {
            return OrderTypesKey;
        }

        var courtClasses = GetValues(CourtClassesKey);
        if (courtClasses.Count > 0 && !courtClasses.Contains(ResolveCourtClass(orderRequest), StringComparer.OrdinalIgnoreCase))
        {
            return CourtClassesKey;
        }

        return null;
    }

    private static string ResolveOrderType(OrderRequestDto orderRequest)
    {
        var courtListTypeCd = orderRequest?.Referral?.CourtListTypeCd;

        if (CourtListTypeDescriptor.IsDeskOrder(courtListTypeCd))
        {
            return CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION;
        }

        return CourtListTypeDescriptor.IsOrder(courtListTypeCd)
            ? CourtListTypeDescriptor.ORDER_DESCRIPTION
            : string.Empty;
    }

    private static string ResolveCourtClass(OrderRequestDto orderRequest)
    {
        if (!Enum.TryParse<CourtClassCd>(orderRequest?.CourtClassCd, true, out var courtClass))
        {
            return string.Empty;
        }

        if (courtClass == CourtClassCd.F)
        {
            return CourtClassCd.F.ToString();
        }

        return courtClass == CourtClassCd.C
            ? CourtClassCd.C.ToString()
            : string.Empty;
    }

    private List<string> GetValues(string key) =>
        [.. (_configuration.GetValue<string>(key) ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
