using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class HttpSortedResultFactory : IHttpSortedResultFactory
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IHttpContentSerializer httpContentSerializer;

        public HttpSortedResultFactory(
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

        public async Task<ISortedResult<T>> Create<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var items = await this.httpContentSerializer.FromHttpContentAsync<IEnumerable<T>>(response.Content, cancellationToken);

            var sorts = SortDescriptorParser.Parse(response.RequestMessage!.RequestUri!);
            return new SortedResult<T>(
                items,
                response.RequestMessage.RequestUri!,
                sorts,
                this);
        }

        public async Task<ISortedResult<T>> Create<T>(Uri address, CancellationToken cancellationToken)
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
