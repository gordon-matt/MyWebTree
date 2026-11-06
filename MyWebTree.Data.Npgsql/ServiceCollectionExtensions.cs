using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWebTree.Data.Npgsql;

/// <summary>
/// Provides extension methods for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores MyWebTree families in PostgreSQL.</summary>
        public IServiceCollection AddMyWebTreeNpgsql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IMyWebTreeDbContextFactory>();
            services.AddSingleton<IMyWebTreeDbContextFactory>(new MyWebTreeDbContextFactory(connectionString));

            return services;
        }
    }
}
