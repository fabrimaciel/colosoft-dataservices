using Refit;
using System.Linq;
using System.Net.Http;

namespace Colosoft.DataServices.Refit
{
    public class RefitServiceFactory : IRefitServiceFactory
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly RefitServiceSettings serviceSettings;
        private readonly IServiceResultFactory serviceResultFactory;

        public RefitServiceFactory(
            RefitServiceSettings serviceSettings,
            IHttpClientFactory httpClientFactory,
            IServiceResultFactory serviceResultFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.serviceSettings = serviceSettings;
            this.serviceResultFactory = serviceResultFactory;
        }

        private string GetHttpClientName<T>() =>
            typeof(T)
                .GetCustomAttributes(typeof(RefitServiceAttribute), true)
                .OfType<RefitServiceAttribute>()
                .FirstOrDefault()?
                .HttpClientName ?? this.serviceSettings?.HttpClientName!;

        public T Create<T>()
        {
            var httpClient = this.httpClientFactory.CreateClient(this.GetHttpClientName<T>());
            return RestService.For<T>(httpClient, new RequestBuilder<T>(this.serviceResultFactory, this.serviceSettings));
        }
    }
}