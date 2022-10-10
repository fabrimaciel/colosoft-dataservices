using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class PagedResultFactory : IPagedResultFactory
    {
        private readonly HttpClient httpClient;
        private readonly IHttpContentSerializer httpContentSerializer;

        public PagedResultFactory(
            HttpClient httpClient,
            IHttpContentSerializer httpContentSerializer)
        {
            this.httpClient = httpClient;
            this.httpContentSerializer = httpContentSerializer;
        }

        public async Task<IPagedResult<T>> Create<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var items = await this.httpContentSerializer.FromHttpContentAsync<IEnumerable<T>>(response.Content, cancellationToken);
            var totalCount = response.GetTotalCount();

            if (!totalCount.HasValue)
            {
                totalCount = items.Count();
            }

            return new PagedResult<T>(items, response.GetLinkHeader(), totalCount.Value, this);
        }

        public async Task<IPagedResult<T>> Create<T>(Uri address, CancellationToken cancellationToken)
        {
            var response = await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, address), cancellationToken);
            response.EnsureSuccessStatusCode();
            return await this.Create<T>(response, cancellationToken);
        }
    }
}
