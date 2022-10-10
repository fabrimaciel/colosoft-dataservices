using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class DataSourceQuery<TFilter, TItem> : DataSourceQuery<TItem>, IDataSource<TFilter, TItem>
    {
        private readonly DataSourceQueryHandler<TFilter, TItem>? query;
        private readonly FilterableDataSourceQueryHandler<TFilter>? filterableQuery;
        private TFilter filter;

        public DataSourceQuery(
           FilterableDataSourceQueryHandler<TFilter> query,
           int pageSize,
           int page,
           Func<object, TItem> itemConverter,
           IEnumerable<SortDescriptor> sorts)
           : base(pageSize, page, itemConverter, sorts)
        {
            this.filterableQuery = query ?? throw new ArgumentNullException(nameof(query));
        }

        public DataSourceQuery(
            DataSourceQueryHandler<TFilter, TItem> query,
            int pageSize,
            int page,
            Func<object, TItem> itemConverter,
            IEnumerable<SortDescriptor> sorts)
            : base(pageSize, page, itemConverter, sorts)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query));
        }

        protected override DataSourceQueryHandler Query => this.GetItems;

        public TFilter Filter
        {
            get => this.filter;
            set
            {
                if (!object.ReferenceEquals(this.filter, value))
                {
                    this.filter = value;
                    this.OnPropertyChanged(nameof(this.Filter));
                }
            }
        }

        private async Task<IEnumerable> GetItems(DataSourceQueryOptions options, CancellationToken cancellationToken)
        {
            if (this.query != null)
            {
                return await this.query.Invoke(this.filter, options, cancellationToken);
            }
            else
            {
                return await this.filterableQuery !.Invoke(this.filter, options, cancellationToken);
            }
        }
    }
}
