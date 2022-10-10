using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IDataSourceProvider<out TItem>
    {
        IDataSource<TItem> DataSource { get; }

        IList<SortDescriptor> Sorts { get; }

        int Page { get; }

        bool IsLoaded { get; }

        Task Refresh(CancellationToken cancellationToken);
    }
}
