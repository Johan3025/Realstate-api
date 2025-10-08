using MongoDB.Driver;

namespace RealState.Data.Persistence
{
    public sealed class MongoContext : IMongoContext
    {
        public IMongoDatabase Database { get; }

        public MongoContext(IMongoClient client, string databaseName)
        {
            Database = client.GetDatabase(databaseName);
            Console.WriteLine($"[MongoContext] Conectado a MongoDB - Base de datos: {databaseName}");
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            Console.WriteLine($"[MongoContext] Obteniendo colección: {name}");
            return Database.GetCollection<T>(name);
        }
    }
}
