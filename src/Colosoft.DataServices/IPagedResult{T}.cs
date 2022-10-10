using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IPagedResult<T> : IEnumerable<T>
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

        Task<IPagedResult<T>?> GetNext(CancellationToken cancellationToken);

        Task<IPagedResult<T>?> GetPrevious(CancellationToken cancellationToken);

        Task<IPagedResult<T>?> GetFirst(CancellationToken cancellationToken);

        Task<IPagedResult<T>?> GetLast(CancellationToken cancellationToken);

        Task<IPagedResult<T>?> GetPage(int page, CancellationToken cancellationToken);
    }
}