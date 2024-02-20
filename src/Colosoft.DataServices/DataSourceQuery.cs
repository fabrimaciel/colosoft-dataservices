using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class DataSourceQuery<TItem> : DataSourceBase<TItem>, IDataSourceQuery
    {
        private readonly int pageSize;
        private readonly int page;
        private readonly IEnumerable<SortDescriptor> sorts;

        public event DataSourceQueryOptionsCreatedHandler DataSourceQueryOptionsCreated;

        protected DataSourceQuery(
            int pageSize,
            int page,
            Func<object, TItem> itemConverter,
            IEnumerable<SortDescriptor> sorts)
            : base(null, itemConverter)
        {
            this.pageSize = pageSize;
            this.sorts = sorts;
            this.page = page;
        }

        public DataSourceQuery(
            DataSourceQueryHandler query,
            int pageSize,
            int page,
            Func<object, TItem> itemConverter,
            IEnumerable<SortDescriptor> sorts)
            : base(null, itemConverter)
        {
            this.Query = query ?? throw new ArgumentNullException(nameof(query));
            this.pageSize = pageSize;
            this.sorts = sorts;
            this.page = page;
        }

        public DataSourceQuery(
            DataSourceQueryHandler<TItem> query,
            int pageSize,
            int page,
            Func<object, TItem> itemConverter,
            IEnumerable<SortDescriptor> sorts)
            : base(null, itemConverter)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            this.Query = new DataSourceQueryHandler(async (options, cancellationToken) => await query.Invoke(options, cancellationToken));
            this.pageSize = pageSize;
            this.sorts = sorts;
            this.page = page;
        }

        protected virtual DataSourceQueryHandler Query { get; }

        public override int PageSize => !this.IsLoaded ? this.pageSize : base.PageSize;

        public override int Page => !this.IsLoaded ? this.page : base.Page;

        public override IEnumerable<SortDescriptor> Sorts => !this.IsLoaded ? this.sorts : base.Sorts;

        protected virtual async Task<DataSourceQueryOptions> CreateQueryOptions(
            IEnumerable<SortDescriptor> sorts,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var options = new DataSourceQueryOptions(sorts, page, pageSize);

            if (this.DataSourceQueryOptionsCreated != null)
            {
                await this.DataSourceQueryOptionsCreated.Invoke(this, new DataSourceQueryOptionsCreatedEventArgs(options), cancellationToken);
            }

            return options;
        }

        protected async Task<IEnumerable> ExecuteQuery(CancellationToken cancellationToken)
        {
            var options = await this.CreateQueryOptions(this.Sorts, this.Page, this.PageSize, cancellationToken);
            return await this.Query.Invoke(options, cancellationToken);
        }

        protected async Task EnsureLoaded(CancellationToken cancellationToken)
        {
            if (!this.IsLoaded)
            {
                this.Items = await this.ExecuteQuery(cancellationToken);
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public override async Task MoveFirstPage(CancellationToken cancellationToken)
        {
            await this.EnsureLoaded(cancellationToken);

            if (this.Page != 1)
            {
                await base.MoveFirstPage(cancellationToken);
            }
        }

        public override async Task MoveLastPage(CancellationToken cancellationToken)
        {
            await this.EnsureLoaded(cancellationToken);

            if (!this.IsLastPage)
            {
                await base.MoveLastPage(cancellationToken);
            }
        }

        public override async Task MoveNextPage(CancellationToken cancellationToken)
        {
            await this.EnsureLoaded(cancellationToken);

            if (!this.IsLastPage)
            {
                await base.MoveNextPage(cancellationToken);
            }
        }

        public override async Task MovePage(int page, CancellationToken cancellationToken)
        {
            if (this.IsLoaded)
            {
                await base.MovePage(page, cancellationToken);
            }
            else
            {
                var options = await this.CreateQueryOptions(this.Sorts, page, this.PageSize, cancellationToken);
                this.Items = await this.Query.Invoke(options, cancellationToken);
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public override async Task MovePreviousPage(CancellationToken cancellationToken)
        {
            await this.EnsureLoaded(cancellationToken);

            if (this.Page > 1)
            {
                await base.MovePreviousPage(cancellationToken);
            }
        }

        public override async Task Refresh(CancellationToken cancellationToken)
        {
            if (this.IsLoaded)
            {
                await base.Refresh(cancellationToken);
            }
            else
            {
                await this.EnsureLoaded(cancellationToken);
                await this.NotifyRefreshed(cancellationToken);
            }
        }

        public override async Task Reset(CancellationToken cancellationToken)
        {
            var options = await this.CreateQueryOptions(this.sorts, Math.Min(this.page, 1), this.pageSize, cancellationToken);
            this.Items = await this.Query.Invoke(options, cancellationToken);
            await this.NotifyDataChanged(cancellationToken);
            await this.NotifyReseted(cancellationToken);
        }

        public override async Task Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken)
        {
            if (this.IsLoaded)
            {
                await base.Sort(sorts, cancellationToken);
            }
            else
            {
                var options = await this.CreateQueryOptions(sorts, this.Page, this.PageSize, cancellationToken);
                this.Items = await this.Query.Invoke(options, cancellationToken);
                await this.NotifyDataChanged(cancellationToken);
            }
        }
    }
}
