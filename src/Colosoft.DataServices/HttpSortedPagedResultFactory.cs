using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class HttpSortedPagedResultFactory : IHttpSortedPagedResultFactory, IHttpSortedResultFactory, IHttpPagedResultFactory
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IHttpContentSerializer httpContentSerializer;

        public HttpSortedPagedResultFactory(
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

            return new HttpSortedPagedResult<T>(
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

        public async Task<IPagedResult<T>> Create<T>(
            PagedResultQueryHandler<T> handler,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var content = await handler.Invoke(new PagedResultQueryOptions(page, pageSize), cancellationToken);

            return new PagedResultQuery<T>(content, handler, page, pageSize);
        }

        async Task<ISortedResult<T>> IHttpSortedResultFactory.Create<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
            await this.Create<T>(response, cancellationToken);

        async Task<ISortedResult<T>> IHttpSortedResultFactory.Create<T>(Uri address, CancellationToken cancellationToken) =>
            await this.Create<T>(address, cancellationToken);

        async Task<IPagedResult<T>> IHttpPagedResultFactory.Create<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
            await this.Create<T>(response, cancellationToken);

        async Task<IPagedResult<T>> IHttpPagedResultFactory.Create<T>(Uri address, CancellationToken cancellationToken) =>
            await this.Create<T>(address, cancellationToken);
    }
}
