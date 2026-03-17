using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;

namespace Scv.Db.Seeders
{
    public class SeederFactory<T> where T : JasperDbContext
    {
        private readonly List<SeederBase<T>> _seeders;
        private readonly ILogger<SeederFactory<T>> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SeederFactory(ILogger<SeederFactory<T>> logger, IServiceProvider serviceProvider)
        {
            _seeders = [];
            _logger = logger;
            _serviceProvider = serviceProvider;
            this.LoadSeeders();
        }

        private void LoadSeeders()
        {
            _logger.LogInformation("Loading seeders...");

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(SeederBase<T>)))
                .ToList();

            foreach (var type in types)
            {
                var seeder = (SeederBase<T>)_serviceProvider.GetRequiredService(type);
                _seeders.Add(seeder);
            }

            _logger.LogInformation("{count} seeders loaded...", types.Count);
        }

        public async Task SeedAsync(T context)
        {
            foreach (var seeder in _seeders.OrderBy(s => s.Order))
            {
                await seeder.SeedAsync(context);
            }
        }
    }
}
