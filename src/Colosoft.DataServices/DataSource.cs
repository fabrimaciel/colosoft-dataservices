using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public static class DataSource
    {
        public static IDataSource<T> ToDataSource<T>(this IEnumerable<T> items) =>
            new DataSource<T>(items, f => (T)f);

        public static IDataSource<TItem> ToDataSource<TItem>(
           this DataSourceQueryHandler<TItem> query,
           int pageSize = 0,
           int page = 1,
           IEnumerable<SortDescriptor>? sorts = null) =>
           new DataSourceQuery<TItem>(query, pageSize, page, f => (TItem)f, sorts ?? Enumerable.Empty<SortDescriptor>());

        public static IDataSource<TFilter, TItem> ToDataSource<TFilter, TItem>(
          this DataSourceQueryHandler<TFilter, TItem> query,
          int pageSize = 0,
          int page = 1,
          IEnumerable<SortDescriptor>? sorts = null) =>
          new DataSourceQuery<TFilter, TItem>(query, pageSize, page, f => (TItem)f, sorts ?? Enumerable.Empty<SortDescriptor>());

        public static IDataSource<TItem> ToDataSource<T, TItem>(this IEnumerable<T> items, Func<T, TItem> itemConverter) =>
            new DataSource<TItem>(items, f => itemConverter((T)f));

        public static IDataSource<TItem> ToDataSource<T, TItem>(
           this DataSourceQueryHandler<T> query,
           Func<T, TItem> itemConverter,
           int pageSize = 0,
           int page = 1,
           IEnumerable<SortDescriptor>? sorts = null) =>
           new DataSourceQuery<TItem>(
               new DataSourceQueryHandler(async (options, cancellationToken) => await query.Invoke(options, cancellationToken)),
               pageSize,
               page,
               f => itemConverter((T)f),
               sorts ?? Enumerable.Empty<SortDescriptor>());

        public static IDataSource<TFilter, TItem> ToDataSource<TFilter, T, TItem>(
           this DataSourceQueryHandler<TFilter, T> query,
           Func<T, TItem> itemConverter,
           int pageSize = 0,
           int page = 1,
           IEnumerable<SortDescriptor>? sorts = null) =>
           new DataSourceQuery<TFilter, TItem>(
               new FilterableDataSourceQueryHandler<TFilter>(async (filter, options, cancellationToken) => await query.Invoke(filter, options, cancellationToken)),
               pageSize,
               page,
               f => itemConverter((T)f),
               sorts ?? Enumerable.Empty<SortDescriptor>());

        public static async Task<IDataSource<TItem>> ToDataSource<T, TItem>(this Task<IEnumerable<T>> items, Func<T, TItem> itemConverter) =>
            (await items).ToDataSource(itemConverter);

        public static IDataSource<TItem> ToDataSource<T, TItem>(this IPagedResult<T> items, Func<T, TItem> itemConverter) =>
            new DataSource<TItem>(items, f => itemConverter((T)f));

        public static async Task<IDataSource<TItem>> ToDataSource<T, TItem>(this Task<IPagedResult<T>> items, Func<T, TItem> itemConverter) =>
           (await items).ToDataSource(itemConverter);

        public static DataSource<TItem> Empty<TItem>() =>
            new DataSource<TItem>(new SimplePagedResult<TItem>(Array.Empty<TItem>(), 0), f => (TItem)f);

        public static DataSource<TItem> Empty<TItem>(int pageSize, int page) =>
           new DataSource<TItem>(new SimplePagedResult<TItem>(Array.Empty<TItem>(), pageSize, page), f => (TItem)f);
    }
}
