using System;
using System.Linq;
using JCCommon.Clients.FileServices;
using Scv.Api.Infrastructure.Options;
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
public class OrderNotificationFilter(OrderNotificationOptions options)
{
    private readonly OrderNotificationOptions _options = options;

    /// <summary>
    /// Returns null when the order matches all filters; otherwise the name of the first filter that rejected it.
    /// </summary>
    public string GetRejectedFilter(OrderRequestDto orderRequest)
    {
        var priorityTypes = _options.GetPriorityTypes();
        if (priorityTypes.Count > 0
            && !priorityTypes.Contains(orderRequest?.Referral?.PriorityType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return OrderNotificationOptions.PriorityTypesKey;
        }

        var orderTypes = _options.GetOrderTypes();
        if (orderTypes.Count > 0 && !orderTypes.Contains(ResolveOrderType(orderRequest), StringComparer.OrdinalIgnoreCase))
        {
            return OrderNotificationOptions.OrderTypesKey;
        }

        var courtClasses = _options.GetCourtClasses();
        if (courtClasses.Count > 0 && !courtClasses.Contains(ResolveCourtClass(orderRequest), StringComparer.OrdinalIgnoreCase))
        {
            return OrderNotificationOptions.CourtClassesKey;
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
}
