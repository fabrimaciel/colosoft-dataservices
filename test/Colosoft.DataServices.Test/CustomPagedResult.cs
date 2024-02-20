using System.Collections;

namespace Colosoft.DataServices.Test
{
    internal class CustomPagedResult<T> : IPagedResult<T>, IPagedResult
    {
        private readonly IEnumerable<T> items;

        public CustomPagedResult(IEnumerable<T> items)
        {
            this.items = items;
        }

        public int TotalCount => this.items.Count();

        public bool HasNextPage => false;

        public bool HasPreviousPage => false;

        public bool HasLastPage => false;

        public bool HasFirstPage => true;

        public int Page => 1;

        public int PageSize => this.items.Count();

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        public Task<IPagedResult<T>?> GetFirst(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IPagedResult<T>?> GetLast(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IPagedResult<T>?> GetNext(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IPagedResult<T>?> GetPage(int page, CancellationToken cancellationToken)
        {
            return Task.FromResult<IPagedResult<T>?>(new CustomPagedResult<T>(this.items));
        }

        public Task<IPagedResult<T>?> GetPrevious(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        Task<IPagedResult?> IPagedResult.GetFirst(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IPagedResult?> IPagedResult.GetLast(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IPagedResult?> IPagedResult.GetNext(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IPagedResult?> IPagedResult.GetPage(int page, CancellationToken cancellationToken)
        {
            return Task.FromResult<IPagedResult?>(new CustomPagedResult<T>(this.items.ToArray()));
        }

        Task<IPagedResult?> IPagedResult.GetPrevious(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
