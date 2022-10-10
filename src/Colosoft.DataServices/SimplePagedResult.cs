using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class SimplePagedResult<T> : IPagedResult, IPagedResult<T>
    {
        private readonly IEnumerable<T> items;

        public SimplePagedResult(IEnumerable<T> items, int pageSize)
            : this(items, pageSize, 1)
        {
        }

        public SimplePagedResult(IEnumerable<T> items, int pageSize, int page)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            this.PageSize = pageSize;
            this.Page = page;
        }

        public int TotalCount => this.items.Count();

        public int Page { get; }

        public int PageSize { get; }

        private int TotalPages => (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);

        public bool HasNextPage => this.Page < this.TotalPages;

        public bool HasPreviousPage => this.Page > 1;

        public bool HasLastPage => this.TotalPages > 1;

        public bool HasFirstPage => this.TotalPages > 1;

        public IEnumerator<T> GetEnumerator()
        {
            if (this.Page == 1)
            {
                return this.items.Take(this.PageSize).GetEnumerator();
            }
            else
            {
                return this.items.Skip((this.Page - 1) * this.PageSize).Take(this.PageSize).GetEnumerator();
            }
        }

        public Task<IPagedResult<T>?> GetFirst(CancellationToken cancellationToken) =>
            Task.FromResult<IPagedResult<T>?>(this.HasFirstPage ? new SimplePagedResult<T>(this.items, this.PageSize, 1) : null);

        public Task<IPagedResult<T>?> GetLast(CancellationToken cancellationToken) =>
            Task.FromResult<IPagedResult<T>?>(this.HasLastPage ? new SimplePagedResult<T>(this.items, this.PageSize, this.TotalPages) : null);

        public Task<IPagedResult<T>?> GetNext(CancellationToken cancellationToken) =>
            Task.FromResult<IPagedResult<T>?>(this.HasNextPage ? new SimplePagedResult<T>(this.items, this.PageSize, this.Page + 1) : null);

        public Task<IPagedResult<T>?> GetPage(int page, CancellationToken cancellationToken) =>
            Task.FromResult<IPagedResult<T>?>(page >= 1 && page <= this.TotalPages ? new SimplePagedResult<T>(this.items, this.PageSize, page) : null);

        public Task<IPagedResult<T>?> GetPrevious(CancellationToken cancellationToken) =>
            Task.FromResult<IPagedResult<T>?>(this.HasPreviousPage ? new SimplePagedResult<T>(this.items, this.PageSize, this.Page - 1) : null);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        async Task<IPagedResult?> IPagedResult.GetFirst(CancellationToken cancellationToken) =>
            (IPagedResult?)await this.GetFirst(cancellationToken);

        async Task<IPagedResult?> IPagedResult.GetLast(CancellationToken cancellationToken) =>
            (IPagedResult?)await this.GetLast(cancellationToken);

        async Task<IPagedResult?> IPagedResult.GetNext(CancellationToken cancellationToken) =>
            (IPagedResult?)await this.GetNext(cancellationToken);

        async Task<IPagedResult?> IPagedResult.GetPage(int page, CancellationToken cancellationToken) =>
            (IPagedResult?)await this.GetPage(page, cancellationToken);

        async Task<IPagedResult?> IPagedResult.GetPrevious(CancellationToken cancellationToken) =>
            (IPagedResult?)await this.GetPrevious(cancellationToken);
    }
}
