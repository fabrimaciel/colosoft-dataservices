using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public delegate Task<IPagedResultContent<TItem>> PagedResultQueryHandler<TItem>(
        PagedResultQueryOptions options,
        CancellationToken cancellationToken);
}
