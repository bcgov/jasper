using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Scv.Api.Helpers.Extensions
{
    public static class DateTimeExtensions
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Configures the DateTimeExtensions to use the provided service provider for dependency injection.
        /// This should be called during application startup.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for resolving dependencies</param>
        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the current HttpContext using IHttpContextAccessor from dependency injection.
        /// </summary>
        /// <returns>The current HttpContext or null if not available</returns>
        private static HttpContext GetCurrentHttpContext()
        {
            if (_serviceProvider == null) return null;
            
            var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext;
        }

        /// <summary>
        /// Gets the current DateTime adjusted to the timezone specified in the X-Timezone header.
        /// Automatically uses the current HTTP context. Falls back to Pacific Time (America/Vancouver) if no header is provided or if the timezone is invalid.
        /// </summary>
        /// <param name="dateTime">The DateTime instance (typically DateTime.Now)</param>
        /// <returns>DateTime adjusted to the specified timezone</returns>
        public static DateTime ToClientTimezone(this DateTime dateTime)
        {
            return dateTime.ToClientTimezone(GetCurrentHttpContext());
        }

        /// <summary>
        /// Gets the current DateTime adjusted to the timezone specified in the X-Timezone header.
        /// Falls back to Pacific Time (America/Vancouver) if no header is provided or if the timezone is invalid.
        /// </summary>
        /// <param name="dateTime">The DateTime instance (typically DateTime.Now)</param>
        /// <param name="httpContext">The HTTP context to read the X-Timezone header from</param>
        /// <returns>DateTime adjusted to the specified timezone</returns>
        public static DateTime ToClientTimezone(this DateTime dateTime, HttpContext httpContext)
        {
            const string defaultTimeZone = "UTC";
            const string timezoneHeader = "X-Timezone";

            try
            {
                // Get timezone from header, fallback to default
                var clientTimezone = httpContext?.Request?.Headers[timezoneHeader].ToString();
                var targetTimezone = string.IsNullOrWhiteSpace(clientTimezone) ? defaultTimeZone : clientTimezone;

                // Convert to target timezone
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, targetTimezone);
            }
            catch (TimeZoneNotFoundException)
            {
                // If the provided timezone is invalid, fallback to Pacific Time
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, defaultTimeZone);
            }
            catch (InvalidTimeZoneException)
            {
                // If the provided timezone is invalid, fallback to Pacific Time
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, defaultTimeZone);
            }
            catch
            {
                // For any other exceptions, return the original datetime
                return dateTime;
            }
        }
    }
}