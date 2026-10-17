using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWebTree.Services;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers MyWebTree services and Extenso repositories bound to <see cref="IMyWebTreeDbContextFactory"/>.
        /// Add a database as well, for example <c>AddMyWebTreeSqlite()</c>.
        /// </summary>
        public IServiceCollection AddMyWebTreeServices(Action<MyWebTreeOptions>? configure = null)
        {
            var optionsBuilder = services.AddOptions<MyWebTreeOptions>().BindConfiguration(MyWebTreeOptions.SectionName);
            if (configure is not null)
            {
                optionsBuilder.Configure(configure);
            }

            services.AddMyWebTreeRepository<FamilyTree>();
            services.AddMyWebTreeRepository<Person>();
            services.AddMyWebTreeRepository<PersonRelationship>();

            services.TryAddScoped<IFamilyTreeService, FamilyTreeService>();
            services.AddHostedService<Hosting.MyWebTreeDatabaseInitializer>();
            return services;
        }

        private void AddMyWebTreeRepository<TEntity>()
            where TEntity : class, IEntity
        {
            // Replace, so a host that calls AddEntityFrameworkRepository() for its own context
            // does not leave MyWebTree reading the wrong database.
            services.Replace(ServiceDescriptor.Singleton<IRepository<TEntity>>(
                sp => new EntityFrameworkRepository<TEntity>(sp.GetRequiredService<IMyWebTreeDbContextFactory>())));
        }
    }
}
