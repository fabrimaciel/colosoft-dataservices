using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface ISortedResult<TItem> : IEnumerable<TItem>
    {
        IEnumerable<SortDescriptor> Sorts { get; }

        Task<ISortedResult<TItem>> Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken);
    }
}