using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class ServiceResultFactory : IServiceResultFactory
    {
        private readonly HttpSortedPagedResultFactory factory;

        public ServiceResultFactory(
            IHttpClientFactory httpClientFactory,
            IHttpContentSerializer httpContentSerializer)
        {
            this.factory = new HttpSortedPagedResultFactory(httpClientFactory, httpContentSerializer);
        }

        public virtual string HttpClientName
        {
            get => this.factory.HttpClientName;
            set => this.factory.HttpClientName = value;
        }

        public async Task<IEnumerable<T>> Create<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
            await this.factory.Create<T>(response, cancellationToken);

        public async Task<IEnumerable<T>> Create<T>(Uri address, CancellationToken cancellationToken) =>
            await this.factory.Create<T>(address, cancellationToken);
    }
}
