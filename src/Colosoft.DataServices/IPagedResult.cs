using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface IPagedResult : IEnumerable
#pragma warning restore CA1010 // Collections should implement generic interface
    {
        int TotalCount { get; }

        bool HasNextPage { get; }

        bool HasPreviousPage { get; }

        bool HasLastPage { get; }

        bool HasFirstPage { get; }

#pragma warning disable CA1721 // Property names should not match get methods
        int Page { get; }
#pragma warning restore CA1721 // Property names should not match get methods
        int PageSize { get; }

        Task<IPagedResult?> GetNext(CancellationToken cancellationToken);

        Task<IPagedResult?> GetPrevious(CancellationToken cancellationToken);

        Task<IPagedResult?> GetFirst(CancellationToken cancellationToken);

        Task<IPagedResult?> GetLast(CancellationToken cancellationToken);

        Task<IPagedResult?> GetPage(int page, CancellationToken cancellationToken);
    }
}