using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public class PagedResultContent<TItem> : IPagedResultContent<TItem>
    {
        public PagedResultContent(IEnumerable<TItem> items, int totalCount)
        {
            this.Items = items;
            this.TotalCount = totalCount;
        }

        public int TotalCount { get; }

        public IEnumerable<TItem> Items { get; }
    }
}
