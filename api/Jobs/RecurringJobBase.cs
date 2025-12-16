using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Scv.Api.Jobs;

public abstract class RecurringJobBase<TJob>(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<TJob> logger) : IRecurringJob
    where TJob : class
{
    /// <summary>
    /// Recurring Job has been configured to PST timezone.
    /// </summary>
    public const string DEFAULT_SCHEDULE = "0 0 0 * * ?"; // 12AM PST

    public abstract string JobName { get; }

    public virtual string CronSchedule => DEFAULT_SCHEDULE;

    public IConfiguration Configuration { get; } = configuration;

    public IAppCache Cache { get; } = cache;

    public IMapper Mapper { get; } = mapper;

    public ILogger<TJob> Logger { get; } = logger;

    public abstract Task Execute();
}
