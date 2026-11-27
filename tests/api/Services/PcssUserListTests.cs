using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class PcssUserListTests
{
    private static readonly TimeSpan FreshDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FallbackWindow = TimeSpan.FromMinutes(5);
    private readonly UserItem _knownUser = new() { GUID = "known-guid", UserId = 42 };
    private readonly Mock<IAuthorizationServicesClient> _client = new();
    private readonly HttpContextAccessor _httpContext = new();
    private readonly TestTimeProvider _time = new();
    private readonly IAppCache _cache = new CachingService(new Lazy<ICacheProvider>(() =>
        new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string> { ["Caching:UserExpiryMinutes"] = "2" }).Build();

    [Fact]
    public async Task FreshListIsSharedAndFallbackDoesNotRenewEitherLifetime()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        Assert.Same(_knownUser, Assert.Single(await service.GetUsers()));

        _time.Advance(FreshDuration - TimeSpan.FromSeconds(1));
        Assert.Equal(new PcssUserMatch(_knownUser), await service.GetUserByGuid(_knownUser.GUID));
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once);

        StallRequests();
        _time.Advance(TimeSpan.FromSeconds(1));
        var firstLookup = service.GetUserByGuid(_knownUser.GUID, forceRefresh: true);
        var concurrentLookup = CreateService().GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await firstLookup);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await concurrentLookup);
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        var secondLookup = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await secondLookup);

        _time.Advance(FallbackWindow - Deadline * 3);
        var expiredLookup = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        await Assert.ThrowsAnyAsync<TimeoutException>(() => expiredLookup);
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ColdCacheTimesOutAtSixtySecondsAndCancelsThePcssRequest()
    {
        CancellationToken outboundToken = default;
        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                outboundToken = token;
                return Stall(token);
            });

        var lookup = CreateService().GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline - TimeSpan.FromSeconds(1));
        Assert.False(outboundToken.IsCancellationRequested);
        Assert.False(lookup.IsCompleted);
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.True(outboundToken.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<TimeoutException>(() => lookup);
    }

    [Fact]
    public async Task TimeoutDoesNotReturnStaleDataToGeneralUserListOrIdLookups()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();
        _time.Advance(FreshDuration);
        StallRequests();

        var list = service.GetUsers();
        var userById = service.GetPcssUserById(_knownUser.UserId.Value);
        _time.Advance(Deadline);

        await Assert.ThrowsAnyAsync<TimeoutException>(() => list);
        await Assert.ThrowsAnyAsync<TimeoutException>(() => userById);
    }

    [Fact]
    public async Task CanceledCallerDoesNotUseFallbackOrCancelAnotherCallersLookup()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();
        _time.Advance(FreshDuration);
        StallRequests();

        using var cancellation = new CancellationTokenSource();
        _httpContext.HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token };
        var canceledLookup = service.GetUserByGuid(_knownUser.GUID);
        _httpContext.HttpContext = null;
        var survivingLookup = service.GetUserByGuid(_knownUser.GUID);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledLookup);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await survivingLookup);
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task NonTimeoutAndInvalidResponsesLeaveLastKnownGoodIntact()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();
        _time.Advance(FreshDuration);

        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetUserByGuid(_knownUser.GUID));

        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("PCSS failure"));
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetUserByGuid(_knownUser.GUID));

        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ICollection<UserItem>)null);
        Assert.Null(await service.GetUserByGuid(_knownUser.GUID));

        ReturnUsers((UserItem)null);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetUserByGuid(_knownUser.GUID));

        StallRequests();
        var timeout = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await timeout);
    }

    [Fact]
    public async Task AuthoritativeEmptyResponseReplacesLastKnownGood()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();
        _time.Advance(FreshDuration);

        ReturnUsers();
        Assert.Null(await service.GetUserByGuid(_knownUser.GUID));

        _time.Advance(FreshDuration);
        StallRequests();
        var timeout = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Null(await timeout);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingNewUserTriggersForcedRefreshAfterFreshOrFallbackLookup(bool initialReadTimesOut)
    {
        var newUser = new UserItem { GUID = "new-guid", UserId = 43 };
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();

        if (initialReadTimesOut)
        {
            _time.Advance(FreshDuration);
            var attempts = 0;
            _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => Interlocked.Increment(ref attempts) == 1
                    ? Stall(token)
                    : Task.FromResult<ICollection<UserItem>>([_knownUser, newUser]));
        }
        else
        {
            ReturnUsers(_knownUser, newUser);
        }

        var lookup = service.GetUserByGuid(newUser.GUID, forceRefresh: true);
        if (initialReadTimesOut)
        {
            _time.Advance(Deadline);
        }

        Assert.Equal(new PcssUserMatch(newUser), await lookup);
        Assert.Equal(new PcssUserMatch(newUser), await service.GetUserByGuid(newUser.GUID));

        _time.Advance(FreshDuration);
        StallRequests();
        var timeout = service.GetUserByGuid(newUser.GUID);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(newUser, UsedFallback: true), await timeout);
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(initialReadTimesOut ? 4 : 3));
    }

    [Fact]
    public async Task CancelingForcedRefreshCancelsTheOutboundRequestWithoutUsingFallback()
    {
        ReturnUsers(_knownUser);
        var service = CreateService();
        await service.GetUsers();

        CancellationToken outboundToken = default;
        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                outboundToken = token;
                return Stall(token);
            });

        using var cancellation = new CancellationTokenSource();
        _httpContext.HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token };
        var lookup = service.GetUserByGuid("new-guid", forceRefresh: true);
        try
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lookup);
            Assert.True(outboundToken.IsCancellationRequested);
        }
        finally
        {
            _httpContext.HttpContext = null;
        }
    }

    [Fact]
    public async Task PrimingPopulatesBothLifetimesAndAFailedPrimeDoesNotReplaceThem()
    {
        var service = CreateService();
        var job = new PrimePcssUserCacheJob(_configuration, _cache, Mock.Of<IMapper>(),
            NullLogger<PrimePcssUserCacheJob>.Instance, service);
        ReturnUsers(_knownUser);
        await job.Execute();
        Assert.Equal(new PcssUserMatch(_knownUser), await service.GetUserByGuid(_knownUser.GUID));
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once);

        StallRequests();
        var failedPrime = job.Execute();
        _time.Advance(Deadline);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => failedPrime);
        Assert.IsAssignableFrom<TimeoutException>(error.InnerException);
        Assert.Equal(new PcssUserMatch(_knownUser), await service.GetUserByGuid(_knownUser.GUID));

        _time.Advance(FreshDuration - Deadline);
        var lookup = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await lookup);
        _client.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task OlderForegroundResponseCannotReplaceNewerAuthoritativeRefresh()
    {
        var olderResponse = new TaskCompletionSource<ICollection<UserItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.SetupSequence(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .Returns(olderResponse.Task)
            .ReturnsAsync(Array.Empty<UserItem>());

        var service = CreateService();
        var firstLookup = service.GetUserByGuid(_knownUser.GUID);
        Assert.Empty(await CreateService().RefreshUsers());
        olderResponse.SetResult(new[] { _knownUser });

        Assert.Null(await firstLookup);
        Assert.Empty(await service.GetUsers());
        _time.Advance(FreshDuration);
        StallRequests();
        var timeout = service.GetUserByGuid(_knownUser.GUID);
        _time.Advance(Deadline);
        Assert.Null(await timeout);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task StalledListBodyIsCanceledAndOnlyTimeoutsCanUseFallback(HttpStatusCode statusCode)
    {
        var body = new StalledResponseContent();
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal("/stage/api/auth/users", request.RequestUri.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = body });
        });
        ReturnUsers(_knownUser);
        await CreateService().GetUsers();
        _time.Advance(FreshDuration);

        var lookup = CreateService(new AuthorizationServicesClient(httpClient)).GetUserByGuid(_knownUser.GUID);
        Assert.True(body.ReadStarted);
        _time.Advance(Deadline);

        if (statusCode is HttpStatusCode.OK or HttpStatusCode.GatewayTimeout)
        {
            Assert.Equal(new PcssUserMatch(_knownUser, UsedFallback: true), await lookup);
        }
        else
        {
            var error = await Assert.ThrowsAsync<ApiException>(() => lookup);
            Assert.Equal((int)statusCode, error.StatusCode);
        }

        Assert.True(body.ReadToken.IsCancellationRequested);
        Assert.True(body.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GatewayTimeoutWithoutEligibleSnapshotPreservesTheHttpFailure(bool primeExpiredSnapshot)
    {
        if (primeExpiredSnapshot)
        {
            ReturnUsers(_knownUser);
            await CreateService().GetUsers();
            _time.Advance(FreshDuration + FallbackWindow);
        }

        using var httpClient = CreateHttpClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
        {
            Content = new StringContent("Gateway timeout")
        }));
        var service = CreateService(new AuthorizationServicesClient(httpClient));

        var error = await Assert.ThrowsAsync<ApiException>(() => service.GetUserByGuid(_knownUser.GUID));

        Assert.Equal((int)HttpStatusCode.GatewayTimeout, error.StatusCode);
    }

    [Fact]
    public async Task CallerCancellationTakesPrecedenceOverGatewayTimeoutFallback()
    {
        ReturnUsers(_knownUser);
        await CreateService().GetUsers();
        _time.Advance(FreshDuration);
        using var cancellation = new CancellationTokenSource();
        _httpContext.HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token };
        _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                cancellation.Cancel();
                return Task.FromException<ICollection<UserItem>>(new ApiException("Gateway timeout",
                    (int)HttpStatusCode.GatewayTimeout, null, null, null));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService().GetUserByGuid(_knownUser.GUID));
    }

    [Fact]
    public async Task MalformedHttpResponseDoesNotUseFallback()
    {
        using var httpClient = CreateHttpClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid-json")
        }));
        ReturnUsers(_knownUser);
        await CreateService().GetUsers();
        _time.Advance(FreshDuration);

        var service = CreateService(new AuthorizationServicesClient(httpClient));
        await Assert.ThrowsAsync<ApiException>(() => service.GetUserByGuid(_knownUser.GUID));
    }

    [Fact]
    public async Task IndividualRolesUseTheExistingClientAndIgnoreTheListDeadline()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal("/stage/api/auth/users/42", request.RequestUri.AbsolutePath);
            return response.Task;
        });
        var service = CreateService(new AuthorizationServicesClient(httpClient));

        var roles = service.GetPcssUserRoleNames(42);
        _time.Advance(Deadline);
        Assert.False(roles.IsCompleted);

        response.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                    "userId": 42,
                    "homeLocationId": 1,
                    "roles": [{ "name": "test-role", "numberOfUsers": 1, "effectiveDate": "2000-01-01" }]
                }
                """)
        });
        var result = await roles;
        Assert.True(result.Succeeded);
        Assert.Equal("test-role", Assert.Single(result.Payload));
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) =>
        new(new PcssUserListResponseHandler { InnerHandler = new ResponseHandler(respond) })
        {
            BaseAddress = new Uri("https://pcss.example.test/stage/")
        };

    private PcssAuthorizationService CreateService(IAuthorizationServicesClient client = null) =>
        new(_configuration, client ?? _client.Object, _cache, NullLogger<PcssAuthorizationService>.Instance, _httpContext, _time);

    private void ReturnUsers(params UserItem[] users) => _client
        .Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

    private void StallRequests() => _client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
        .Returns((CancellationToken token) => Stall(token));

    private static async Task<ICollection<UserItem>> Stall(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return Array.Empty<UserItem>();
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly List<ScheduledTimer> _timers = [];
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow()
        {
            lock (_timers)
            {
                return _utcNow;
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, period);
            lock (_timers)
            {
                var timer = new ScheduledTimer(_utcNow + dueTime, callback, state);
                _timers.Add(timer);
                return timer;
            }
        }

        public void Advance(TimeSpan amount)
        {
            ScheduledTimer[] due;
            lock (_timers)
            {
                _utcNow += amount;
                due = _timers.Where(timer => timer.DueAt <= _utcNow).ToArray();
                _timers.RemoveAll(timer => timer.DueAt <= _utcNow);
            }

            foreach (var timer in due)
            {
                timer.Fire();
            }
        }

        private sealed class ScheduledTimer(DateTimeOffset dueAt, TimerCallback callback, object state) : ITimer
        {
            private volatile bool _disposed;
            public DateTimeOffset DueAt { get; } = dueAt;

            public void Fire()
            {
                if (!_disposed)
                {
                    callback(state);
                }
            }

            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() => _disposed = true;
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class ResponseHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request);
    }

    private sealed class StalledResponseContent : HttpContent
    {
        public bool ReadStarted { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public bool Disposed { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            throw new InvalidOperationException("The response body must be read with cancellation.");

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            ReadStarted = true;
            ReadToken = cancellationToken;
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = disposing;
            base.Dispose(disposing);
        }
    }
}
