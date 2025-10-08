using MongoDB.Driver;

namespace RealState.Data.Persistence
{
    public interface IMongoContext
    {
        IMongoDatabase Database { get; }
        IMongoCollection<T> GetCollection<T>(string name);
    }
}
