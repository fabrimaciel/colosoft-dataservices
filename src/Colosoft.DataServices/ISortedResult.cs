using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface ISortedResult : IEnumerable
#pragma warning restore CA1010 // Collections should implement generic interface
    {
        IEnumerable<SortDescriptor> Sorts { get; }

        Task<ISortedResult> Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken);
    }
}