using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface IDataSource : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable
#pragma warning restore CA1010 // Collections should implement generic interface
    {
        bool HasFirstPage { get; }

        bool HasLastPage { get; }

        bool HasNextPage { get; }

        bool HasPreviousPage { get; }

        int Page { get; }

        int PageSize { get; }

        int TotalCount { get; }

        bool IsLoaded { get; }

        IEnumerable<SortDescriptor> Sorts { get; }

        Task MoveFirstPage(CancellationToken cancellationToken);

        Task MoveLastPage(CancellationToken cancellationToken);

        Task MoveNextPage(CancellationToken cancellationToken);

        Task MovePreviousPage(CancellationToken cancellationToken);

        Task MovePage(int page, CancellationToken cancellationToken);

        Task Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken);

        Task Refresh(CancellationToken cancellationToken);

        Task Reset(CancellationToken cancellationToken);
    }
}
