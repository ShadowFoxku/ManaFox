using ManaFox.Databases.Core.Configuration;
using ManaFox.Databases.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManaFox.Databases.Extensions
{
    public static class RuneReaderExtentions
    {
        public static IServiceCollection AddRuneReaderConfig(this IServiceCollection services, IConfigurationSection connectionSection)
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

        public static IServiceCollection AddRuneReaderConfig(this IServiceCollection services, string defaultConnStringName, IConfigurationSection connectionSection)
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
