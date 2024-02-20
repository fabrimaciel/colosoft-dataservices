using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class PagedResultQuery<T> : IPagedResult<T>, IPagedResult, IResettableResult
    {
        private readonly IEnumerable<T> items;
        private readonly PagedResultQueryHandler<T> handler;

        public PagedResultQuery(
            IPagedResultContent<T> content,
            PagedResultQueryHandler<T> handler,
            int page,
            int pageSize)
        {
            this.items = content.Items;
            this.handler = handler;
            this.Page = page;
            this.PageSize = pageSize;
            this.TotalCount = content.TotalCount;

            this.TotalPages = (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
        }

        public PagedResultQuery(
            IEnumerable<T> items,
            PagedResultQueryHandler<T> handler,
            int page,
            int pageSize,
            int totalCount)
        {
            this.items = items;
            this.handler = handler;
            this.Page = page;
            this.PageSize = pageSize;
            this.TotalCount = totalCount;

            this.TotalPages = (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
        }

        public int TotalCount { get; }

        protected int TotalPages { get; }

        public bool HasNextPage => this.Page + 1 <= this.TotalPages;

        public bool HasPreviousPage => this.Page > 1;

        public bool HasLastPage => this.Page == this.TotalPages;

        public bool HasFirstPage => this.Page > 1;

        public int Page { get; }

        public int PageSize { get; }

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        public Task<IPagedResult<T>?> GetFirst(CancellationToken cancellationToken) =>
            this.GetPage(1, cancellationToken);

        public Task<IPagedResult<T>?> GetLast(CancellationToken cancellationToken) =>
            this.GetPage(this.TotalPages, cancellationToken);

        public Task<IPagedResult<T>?> GetNext(CancellationToken cancellationToken) =>
            this.GetPage(this.Page + 1, cancellationToken);

        public async Task<IPagedResult<T>?> GetPage(int page, CancellationToken cancellationToken)
        {
            var content = await this.handler.Invoke(
                new PagedResultQueryOptions(page, this.PageSize),
                cancellationToken);

            return new PagedResultQuery<T>(content, this.handler, page, this.PageSize);
        }

        public Task<IPagedResult<T>?> GetPrevious(CancellationToken cancellationToken) =>
            this.GetPage(this.Page - 1, cancellationToken);

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        async Task<IPagedResult?> IPagedResult.GetNext(CancellationToken cancellationToken) =>
           (IPagedResult?)(await this.GetNext(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPrevious(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPrevious(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetFirst(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetFirst(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetLast(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetLast(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPage(int page, CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPage(page, cancellationToken));

        public virtual async Task<IResettableResult?> Reset(CancellationToken cancellationToken)
        {
            return (IResettableResult?)(await this.GetPage(1, cancellationToken));
        }
    }
}
