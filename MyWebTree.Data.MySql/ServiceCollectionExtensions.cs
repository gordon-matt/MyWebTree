using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWebTree.Data.MySql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores MyWebTree families in MySQL.</summary>
        public IServiceCollection AddMyWebTreeMySql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IMyWebTreeDbContextFactory>();
            services.AddSingleton<IMyWebTreeDbContextFactory>(new MyWebTreeDbContextFactory(connectionString));

            return services;
        }
    }
}
