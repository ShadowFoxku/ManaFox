using ManaFox.Databases.TSQL.Interfaces;
using ManaFox.Databases.TSQL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManaFox.Databases.TSQL.Extensions
{
    public static class RuneReaderExtentions
    {
        public static IServiceCollection AddRuneReaderDb(this IServiceCollection services, IConfigurationSection connectionSection)
        {
            services.AddSingleton<IRuneReaderConfiguration>(sp =>
            {
                var strings = connectionSection
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value!);

                return new RuneReaderConfiguration(strings);
            });
            return services;
        }

        public static IServiceCollection AddRuneReaderDb(this IServiceCollection services, string defaultConnStringName, IConfigurationSection connectionSection)
        {
            services.AddSingleton<IRuneReaderConfiguration>(sp =>
            {
                var strings = connectionSection
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value!);

                return new RuneReaderConfiguration(defaultConnStringName, strings);
            });
            return services;
        }
    }
}
