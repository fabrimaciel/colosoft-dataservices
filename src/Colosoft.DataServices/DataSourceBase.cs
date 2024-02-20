using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public abstract class DataSourceBase<TItem> : IDataSource<TItem>, IDataSourceEventsListener, IDisposable
    {
        private readonly Func<object, TItem> itemConverter;
        private readonly IEnumerable? sourceItems;
        private readonly List<IDataSourceObserver> observers = new List<IDataSourceObserver>();

        private IEnumerable? items;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        protected DataSourceBase(
            IEnumerable? items,
            Func<object, TItem> itemConverter)
        {
            this.sourceItems = items;
            this.items = items;
            this.itemConverter = itemConverter ?? throw new ArgumentNullException(nameof(itemConverter));

            if (this.sourceItems is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged += this.SourceItemsCollectionChanged;
            }
        }

        ~DataSourceBase() => this.Dispose(false);

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

        public virtual int TotalCount
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

        protected virtual void Add(IDataSourceObserver observer) =>
            this.observers.Add(observer ?? throw new ArgumentNullException(nameof(observer)));

        protected virtual bool Remove(IDataSourceObserver observer) =>
            this.observers.Remove(observer);

        void IDataSourceEventsListener.Add(IDataSourceObserver observer) =>
            this.observers.Add(observer ?? throw new ArgumentNullException(nameof(observer)));

        bool IDataSourceEventsListener.Remove(IDataSourceObserver observer) =>
            this.observers.Remove(observer);

#pragma warning disable S2971 // LINQ expressions should be simplified
        protected Task NotifyObservers(Func<IDataSourceObserver, Task> callback) =>
            Task.WhenAll(this.observers.ToArray().Select(callback));
#pragma warning restore S2971 // LINQ expressions should be simplified

        protected virtual Task NotifyRefreshed(CancellationToken cancellationToken) =>
            this.NotifyObservers(f => f.OnRefreshed(cancellationToken));

        protected virtual Task NotifyReseted(CancellationToken cancellationToken) =>
            this.NotifyObservers(f => f.OnReseted(cancellationToken));

        protected virtual Task NotifyDataChanged(CancellationToken cancellationToken) =>
            this.NotifyObservers(f => f.OnDataChanged(cancellationToken));

        public virtual async Task MoveNextPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasNextPage)
            {
                this.Items = (await this.PagedResult.GetNext(cancellationToken)) !;
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task MovePreviousPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasPreviousPage)
            {
                this.Items = (await this.PagedResult.GetPrevious(cancellationToken)) !;
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task MoveFirstPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasFirstPage)
            {
                this.Items = (await this.PagedResult.GetFirst(cancellationToken)) !;
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task MoveLastPage(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null && this.PagedResult.HasLastPage)
            {
                this.Items = (await this.PagedResult.GetLast(cancellationToken)) !;
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task MovePage(int page, CancellationToken cancellationToken)
        {
            if (this.PagedResult != null)
            {
                this.Items = (await this.PagedResult.GetPage(page, cancellationToken)) !;
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task Sort(IEnumerable<SortDescriptor> sorts, CancellationToken cancellationToken)
        {
            if (this.SortedResult != null)
            {
                this.Items = await this.SortedResult.Sort(sorts, cancellationToken);
                await this.NotifyDataChanged(cancellationToken);
            }
        }

        public virtual async Task Refresh(CancellationToken cancellationToken)
        {
            if (this.PagedResult != null)
            {
                this.Items = (await this.PagedResult.GetPage(this.Page, cancellationToken)) !;
            }

            await this.NotifyDataChanged(cancellationToken);
            await this.NotifyRefreshed(cancellationToken);
        }

        public virtual async Task Reset(CancellationToken cancellationToken)
        {
            if (this.Items is IResettableResult resettableResult)
            {
                this.Items = (IEnumerable?)await resettableResult.Reset(cancellationToken);
                await this.NotifyDataChanged(cancellationToken);
            }

            await this.NotifyReseted(cancellationToken);
        }

        protected void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private void SourceItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.CollectionChanged?.Invoke(this, e);
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

        protected virtual void Dispose(bool disposing)
        {
            if (this.sourceItems is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged -= this.SourceItemsCollectionChanged;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
