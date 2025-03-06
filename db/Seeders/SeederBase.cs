using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Scv.Db.Seeders
{
    public abstract class SeederBase<T>(ILogger logger) where T : DbContext
    {
        public ILogger Logger { get; } = logger;

        public abstract int Order { get; }

        protected abstract Task ExecuteAsync(T context);

        public async Task SeedAsync(T context)
        {
            await this.ExecuteAsync(context);
        }
    }
}
