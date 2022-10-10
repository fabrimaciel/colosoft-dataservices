using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class SortedResult<T> : ISortedResult<T>, ISortedResult, IResettableResult
    {
        private readonly IEnumerable<T> items;
        private readonly ISortedResultFactory sortedResultFactory;
        private readonly Uri requestUrl;

        public SortedResult(
            IEnumerable<T> items,
            Uri requestUrl,
            IEnumerable<SortDescriptor> sorts,
            ISortedResultFactory sortedResultFactory)
        {
            this.items = items;
            this.requestUrl = requestUrl;
            this.Sorts = sorts;
            this.sortedResultFactory = sortedResultFactory;
        }

        public IEnumerable<SortDescriptor> Sorts { get; }

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        public Task<ISortedResult<T>> Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken)
        {
            var link = this.requestUrl.ToString();
            var sortParameter = sorts != null && sorts.Any() ?
                $"sort={System.Web.HttpUtility.UrlEncode(SortDescriptorFormatter.Format(sorts))}" :
                string.Empty;

            if (SortedResultConstants.SortRegex.IsMatch(link))
            {
                link = SortedResultConstants.SortRegex.Replace(link, sortParameter);
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

            return this.sortedResultFactory.Create<T>(new Uri(link), cancellationToken);
        }

        public virtual async Task<IResettableResult?> Reset(CancellationToken cancellationToken)
        {
            return (IResettableResult?)(await this.Sort(Enumerable.Empty<SortDescriptor>(), cancellationToken));
        }

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        async Task<ISortedResult> ISortedResult.Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken) =>
            (ISortedResult)(await this.Sort(sorts, cancellationToken));
    }
}
