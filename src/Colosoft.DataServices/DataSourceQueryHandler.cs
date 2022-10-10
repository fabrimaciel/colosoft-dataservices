using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public delegate Task<IEnumerable> DataSourceQueryHandler(DataSourceQueryOptions options, CancellationToken cancellationToken);

    public delegate Task<IEnumerable<TItem>> DataSourceQueryHandler<TItem>(DataSourceQueryOptions options, CancellationToken cancellationToken);

    public delegate Task<IEnumerable<TItem>> DataSourceQueryHandler<TFilter, TItem>(TFilter filter, DataSourceQueryOptions options, CancellationToken cancellationToken);

    public delegate Task<IEnumerable> FilterableDataSourceQueryHandler<TFilter>(TFilter filter, DataSourceQueryOptions options, CancellationToken cancellationToken);
}
