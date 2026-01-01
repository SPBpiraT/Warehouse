using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddContext(this IServiceCollection services, string connectionString)
        {
            var migrationsAssemblyName = typeof(WarehouseDbContext).Assembly.GetName().Name;

            services.AddDbContext<WarehouseDbContext>(options =>
            {
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly(migrationsAssemblyName));
            });

            return services;
        }
    }
}
