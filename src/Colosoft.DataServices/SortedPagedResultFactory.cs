using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class SortedPagedResultFactory : ISortedPagedResultFactory, ISortedResultFactory, IPagedResultFactory
    {
        private readonly HttpClient httpClient;
        private readonly IHttpContentSerializer httpContentSerializer;

        public SortedPagedResultFactory(
            HttpClient httpClient,
            IHttpContentSerializer httpContentSerializer)
        {
            this.httpClient = httpClient;
            this.httpContentSerializer = httpContentSerializer;
        }

        public async Task<ISortedPagedResult<T>> Create<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            IEnumerable<T> items;
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                items = Enumerable.Empty<T>();
            }
            else
            {
                items = await this.httpContentSerializer.FromHttpContentAsync<IEnumerable<T>>(response.Content, cancellationToken);
            }

            return this.Create(response, items);
        }

        public ISortedPagedResult<T> Create<T>(HttpResponseMessage response, IEnumerable<T> items)
        {
            var totalCount = response.GetTotalCount();

            if (!totalCount.HasValue)
            {
                totalCount = items.Count();
            }

            var sorts = SortDescriptorParser.Parse(response.RequestMessage?.RequestUri!);

            return new SortedPagedResult<T>(
                items,
                response.GetLinkHeader(),
                totalCount.Value,
                response.RequestMessage?.RequestUri!,
                sorts,
                this,
                this);
        }

        public async Task<ISortedPagedResult<T>> Create<T>(Uri address, CancellationToken cancellationToken)
        {
            var response = await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, address), cancellationToken);
            response.EnsureSuccessStatusCode();
            return await this.Create<T>(response, cancellationToken);
        }

        async Task<ISortedResult<T>> ISortedResultFactory.Create<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
            await this.Create<T>(response, cancellationToken);

        async Task<ISortedResult<T>> ISortedResultFactory.Create<T>(Uri address, CancellationToken cancellationToken) =>
            await this.Create<T>(address, cancellationToken);

        async Task<IPagedResult<T>> IPagedResultFactory.Create<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
            await this.Create<T>(response, cancellationToken);

        async Task<IPagedResult<T>> IPagedResultFactory.Create<T>(Uri address, CancellationToken cancellationToken) =>
            await this.Create<T>(address, cancellationToken);
    }
}
