using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;

namespace Scv.Db.Seeders
{
    public class SeederFactory<T> where T : JasperDbContext
    {
        private readonly ILogger<SeederFactory<T>> _logger;
        private readonly List<SeederBase<T>> _seeders;

        public SeederFactory(ILogger<SeederFactory<T>> logger)
        {
            _logger = logger;
            _seeders = [];
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
                _logger.LogInformation($"Creating instance of {type.Name}", type.Name);
                _seeders.Add((SeederBase<T>)Activator.CreateInstance(type, this._logger));
            }

            _logger.LogInformation($"{types.Count} seeders loaded...");
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
