using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWebTree.Data.Sql;

/// <summary>
/// Provides extension methods for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores MyWebTree families in SQL Server.</summary>
        public IServiceCollection AddMyWebTreeSqlServer(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IMyWebTreeDbContextFactory>();
            services.AddSingleton<IMyWebTreeDbContextFactory>(new MyWebTreeDbContextFactory(connectionString));

            return services;
        }
    }
}
