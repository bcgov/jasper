using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;

namespace Scv.Db.Seeders
{
    internal abstract class SeederBase<T>(ILogger logger) where T : JasperDbContext
    {
        public ILogger Logger { get; } = logger;

        public int Order { get; protected set; }

        protected abstract Task ExecuteAsync(T context);

        public async Task SeedAsync(T context)
        {
            await this.ExecuteAsync(context);
        }
    }
}
