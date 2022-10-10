using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public abstract class DataSourceBase<TItem> : IDataSource<TItem>
    {
        private readonly Func<object, TItem> itemConverter;
        private IEnumerable? items;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        protected DataSourceBase(
            IEnumerable? items,
            Func<object, TItem> itemConverter)
        {
            this.items = items;
            this.itemConverter = itemConverter ?? throw new ArgumentNullException(nameof(itemConverter));
        }

        public IEnumerable<TItem> Data => this;

        protected virtual IEnumerable? Items
        {
            get => this.items;
            set
            {
                if (this.items != value)
                {
                    var lastState = new[]
                    {
                        this.HasNextPage,
                        this.HasPreviousPage,
                        this.HasFirstPage,
                        this.HasLastPage,
                    };

                    this.items = value;

                    if (lastState[0] != this.HasNextPage)
                    {
                        this.OnPropertyChanged(nameof(this.HasNextPage));
                    }

                    if (lastState[1] != this.HasPreviousPage)
                    {
                        this.OnPropertyChanged(nameof(this.HasPreviousPage));
                    }

                    if (lastState[2] != this.HasFirstPage)
                    {
                        this.OnPropertyChanged(nameof(this.HasFirstPage));
                    }

                    if (lastState[3] != this.HasLastPage)
                    {
                        this.OnPropertyChanged(nameof(this.HasLastPage));
                    }

                    this.OnPropertyChanged(nameof(this.Items), nameof(this.Data), nameof(this.TotalCount), nameof(this.Page));

                    this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        public bool IsLoaded => this.Items != null;

        public IPagedResult? PagedResult => this.Items as IPagedResult;

        public ISortedResult? SortedResult => this.Items as ISortedResult;

        public bool HasNextPage => (this.PagedResult?.HasNextPage).GetValueOrDefault();

        public bool HasPreviousPage => (this.PagedResult?.HasPreviousPage).GetValueOrDefault();

        public bool HasFirstPage => (this.PagedResult?.HasFirstPage).GetValueOrDefault();

        public bool HasLastPage => (this.PagedResult?.HasLastPage).GetValueOrDefault();

        public virtual int Page => this.PagedResult?.Page ?? 1;

        public virtual int PageSize => this.PagedResult?.PageSize ?? this.TotalCount;

        public int TotalCount
        {
            get
            {
                if (this.PagedResult != null)
                {
                    return this.PagedResult.TotalCount;
                }

                if (this.Items is ICollection collection)
                {
                    return collection.Count;
                }

                if (this.Items != null)
                {
                    var count = 0;
                    foreach (var item in this.Items)
                    {
                        count++;
                    }

                    return count;
                }

                return 0;
            }
        }

        public int LastPage
        {
            get
            {
                var pageSize = this.PageSize;
                var totalCount = this.TotalCount;
                if (pageSize == 0 || totalCount == 0)
                {
                    return 1;
                }

                return (int)Math.Ceiling(totalCount / (float)pageSize);
            }
        }

        public bool IsLastPage => this.Page == this.LastPage;

        public virtual IEnumerable<SortDescriptor> Sorts => this.SortedResult?.Sorts ?? Array.Empty<SortDescriptor>();

        public virtual async Task MoveNextPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasNextPage)
            {
                this.Items = (await this.PagedResult.GetNext(cancellationToken)) !;
            }
        }

        public virtual async Task MovePreviousPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasPreviousPage)
            {
                this.Items = (await this.PagedResult.GetPrevious(cancellationToken)) !;
            }
        }

        public virtual async Task MoveFirstPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasFirstPage)
            {
                this.Items = (await this.PagedResult.GetFirst(cancellationToken)) !;
            }
        }

        public virtual async Task MoveLastPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasLastPage)
            {
                this.Items = (await this.PagedResult.GetFirst(cancellationToken)) !;
            }
        }

        public virtual async Task MovePage(int page, CancellationToken cancellationToken)
        {
            if (this.PagedResult != null)
            {
                this.Items = (await this.PagedResult.GetPage(page, cancellationToken)) !;
            }
        }

        public virtual async Task Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken)
        {
            if (this.SortedResult != null)
            {
                this.Items = await this.SortedResult.Sort(sorts, cancellationToken);
            }
        }

        public virtual async Task Refresh(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null)
            {
                this.Items = (await this.PagedResult.GetPage(this.Page, cancellationToken)) !;
            }
        }

        public virtual async Task Reset(CancellationToken cancellationToken)
        {
            if (this.Items is IResettableResult resettableResult)
            {
                this.Items = (IEnumerable?)await resettableResult.Reset(cancellationToken);
            }
        }

        protected void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            if (this.Items != null)
            {
                foreach (var item in this.Items)
                {
                    yield return this.itemConverter(item);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
