using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RealState.Data.Persistence;
using RealState.Data.Repositories;
using RealState.Data.Settings;
using RealState.Services.Abstractions;

namespace RealState.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var settings = new MongoDbSettings();
            config.GetSection("MongoDbSettings").Bind(settings);

            services.AddSingleton(settings);

            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
            services.AddSingleton<IMongoContext>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return new MongoContext(client, settings.DatabaseName);
            });

            services.AddScoped<IPropertyRepository, PropertyRepository>();

            return services;
        }
    }
}
