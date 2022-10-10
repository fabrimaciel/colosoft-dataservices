using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class ProxyPagedResult<TSource, TTarget> : IPagedResult, IPagedResult<TTarget>
    {
        private readonly IPagedResult<TSource> source;
        private readonly Func<TSource, TTarget> itemConverter;

        public ProxyPagedResult(IPagedResult<TSource> source, Func<TSource, TTarget> itemConverter)
        {
            this.source = source;
            this.itemConverter = itemConverter;
        }

        public int TotalCount => this.source.TotalCount;

        public int Page => this.source.Page;

        public int PageSize => this.source.PageSize;

        public bool HasNextPage => this.source.HasNextPage;

        public bool HasPreviousPage => this.source.HasPreviousPage;

        public bool HasLastPage => this.source.HasLastPage;

        public bool HasFirstPage => this.source.HasFirstPage;

        public IEnumerator<TTarget> GetEnumerator() =>
            this.source.Select(f => this.itemConverter(f)).GetEnumerator();

        public async Task<IPagedResult<TTarget>?> GetFirst(CancellationToken cancellationToken) =>
            (await this.source.GetFirst(cancellationToken))?.ToProxy(this.itemConverter);

        public async Task<IPagedResult<TTarget>?> GetLast(CancellationToken cancellationToken) =>
            (await this.source.GetLast(cancellationToken))?.ToProxy(this.itemConverter);

        public async Task<IPagedResult<TTarget>?> GetNext(CancellationToken cancellationToken) =>
            (await this.source.GetNext(cancellationToken))?.ToProxy(this.itemConverter);

        public async Task<IPagedResult<TTarget>?> GetPage(int page, CancellationToken cancellationToken) =>
            (await this.source.GetPage(page, cancellationToken))?.ToProxy(this.itemConverter);

        public async Task<IPagedResult<TTarget>?> GetPrevious(CancellationToken cancellationToken) =>
            (await this.source.GetPrevious(cancellationToken))?.ToProxy(this.itemConverter);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        async Task<IPagedResult?> IPagedResult.GetFirst(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetFirst(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetLast(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetLast(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetNext(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetNext(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPage(int page, CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPage(page, cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPrevious(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPrevious(cancellationToken));
    }
}
