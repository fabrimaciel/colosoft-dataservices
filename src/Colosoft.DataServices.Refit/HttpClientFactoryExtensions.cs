using Microsoft.Extensions.DependencyInjection;
using Refit;
using System;

namespace Colosoft.DataServices.Refit
{
    public static class HttpClientFactoryExtensions
    {
        public static IServiceCollection AddRefitClient<T>(this IServiceCollection services, RefitSettings? settings = null)
            where T : class
        {
            return AddRefitClient<T>(services, _ => settings);
        }

        public static IServiceCollection AddRefitClient<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            services.AddSingleton(provider => new SettingsFor<T>(settingsAction?.Invoke(provider)));
            services.AddScoped(
                provider =>
                    new RequestBuilder<T>(
                        provider.GetRequiredService<IServiceResultFactory>(),
                        provider.GetRequiredService<SettingsFor<T>>().Settings));

            return services
                .AddTransient(serviceProvider =>
                    serviceProvider.GetRequiredService<IRefitServiceFactory>() !.Create<T>());
        }
    }
}