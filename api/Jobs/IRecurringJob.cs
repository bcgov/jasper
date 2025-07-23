using System.Threading.Tasks;

namespace Scv.Api.Jobs;

public interface IRecurringJob
{
    string JobName { get; }
    string CronSchedule { get; }
    Task Execute();
}
