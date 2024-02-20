using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class HttpSortedPagedResult<T> : HttpPagedResult<T>, ISortedPagedResult<T>, ISortedResult
    {
        private readonly IHttpSortedResultFactory sortedResultFactory;
        private readonly Uri requestUrl;

        public HttpSortedPagedResult(
            IEnumerable<T> items,
            LinkHeader linkHeader,
            int totalCount,
            Uri requestUrl,
            IEnumerable<SortDescriptor> sorts,
            IHttpSortedResultFactory sortedResultFactory,
            IHttpPagedResultFactory pagedResultFactory)
            : base(items, linkHeader, totalCount, pagedResultFactory)
        {
            this.requestUrl = requestUrl;
            this.Sorts = sorts;
            this.sortedResultFactory = sortedResultFactory;
        }

        public IEnumerable<SortDescriptor> Sorts { get; }

        public Task<ISortedResult<T>> Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken)
        {
            var link = this.CreateSortLink(sorts, this.requestUrl.ToString());
            return this.sortedResultFactory.Create<T>(new Uri(link), cancellationToken);
        }

        private string CreateSortLink(IEnumerable<SortDescriptor> sorts, string link)
        {
            var sortParameter = sorts != null && sorts.Any()
                ? $"sort={System.Web.HttpUtility.UrlEncode(SortDescriptorFormatter.Format(sorts))}"
                : null;

            if (SortedPageResult.SortRegex.IsMatch(link))
            {
                link = SortedPageResult.SortRegex.Replace(link, sortParameter ?? string.Empty);
            }
            else if (sortParameter != null)
            {
                if (string.IsNullOrEmpty(this.requestUrl.Query))
                {
                    link += $"?{sortParameter}";
                }
                else
                {
                    link += $"&{sortParameter}";
                }
            }

            return link;
        }

        public override async Task<IResettableResult?> Reset(CancellationToken cancellationToken)
        {
            var pageLink = this.CreatePageLink(1);

            if (string.IsNullOrEmpty(pageLink))
            {
                return this;
            }

            var link = this.CreateSortLink(Enumerable.Empty<SortDescriptor>(), pageLink !);
            return (IResettableResult?)(await this.sortedResultFactory.Create<T>(new Uri(link), cancellationToken));
        }

        async Task<ISortedResult> ISortedResult.Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken) =>
            (ISortedResult)(await this.Sort(sorts, cancellationToken));
    }
}
