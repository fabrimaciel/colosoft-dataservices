using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IPagedResultFactory
    {
        Task<IPagedResult<T>> Create<T>(
            PagedResultQueryHandler<T> handler,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
    }
}