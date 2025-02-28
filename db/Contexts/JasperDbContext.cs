using MongoDB.Driver;
using Scv.Db.Models;

namespace Scv.Db.Contexts
{
    public class JasperDbContext
    {
        private readonly IMongoDatabase _database;

        public IMongoCollection<Permission> Permissions => _database.GetCollection<Permission>("permissions");

        public JasperDbContext(string connectionString, string dbName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(dbName);
        }
    }
}
