using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class HttpPagedResultFactory : IHttpPagedResultFactory
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IHttpContentSerializer httpContentSerializer;

        public HttpPagedResultFactory(
            IHttpClientFactory httpClientFactory,
            IHttpContentSerializer httpContentSerializer)
        {
            this.httpClientFactory = httpClientFactory;
            this.httpContentSerializer = httpContentSerializer;
        }

        public virtual string HttpClientName { get; set; } = Options.DefaultName;

        protected virtual HttpClient CreateHttpClient(Uri address)
        {
            return this.httpClientFactory.CreateClient(this.HttpClientName);
        }

        public async Task<IPagedResult<T>> Create<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var items = await this.httpContentSerializer.FromHttpContentAsync<IEnumerable<T>>(response.Content, cancellationToken);
            var totalCount = response.GetTotalCount();

            if (!totalCount.HasValue)
            {
                totalCount = items.Count();
            }

            return new HttpPagedResult<T>(items, response.GetLinkHeader(), totalCount.Value, this);
        }

        public async Task<IPagedResult<T>> Create<T>(Uri address, CancellationToken cancellationToken)
        {
            using (var httpClient = this.CreateHttpClient(address))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, address))
                {
                    var response = await httpClient.SendAsync(request, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return await this.Create<T>(response, cancellationToken);
                }
            }
        }
    }
}
