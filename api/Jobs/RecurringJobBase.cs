using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Jobs;

public abstract class RecurringJobBase<TJob>(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<TJob> logger) : IRecurringJob
    where TJob : class
{
    public const string DEFAULT_SCHEDULE = "0 7 * * *"; // 7AM UTC / 12AM PST

    public abstract string JobName { get; }

    public virtual string CronSchedule => DEFAULT_SCHEDULE;

    public IConfiguration Configuration { get; } = configuration;

    public IAppCache Cache { get; } = cache;

    public IMapper Mapper { get; } = mapper;

    public ILogger<TJob> Logger { get; } = logger;

    public abstract Task Execute();
}
