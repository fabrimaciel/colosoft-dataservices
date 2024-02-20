using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public interface IPagedResultContent<out TItem>
    {
        int TotalCount { get; }

        IEnumerable<TItem> Items { get; }
    }
}