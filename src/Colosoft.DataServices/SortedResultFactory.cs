using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class SortedResultFactory : ISortedResultFactory
    {
        private readonly HttpClient httpClient;
        private readonly IHttpContentSerializer httpContentSerializer;

        public SortedResultFactory(
            HttpClient httpClient,
            IHttpContentSerializer httpContentSerializer)
        {
            this.httpClient = httpClient;
            this.httpContentSerializer = httpContentSerializer;
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
            var response = await this.httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, address),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            return await this.Create<T>(response, cancellationToken);
        }
    }
}
