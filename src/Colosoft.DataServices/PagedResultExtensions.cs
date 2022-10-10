using System;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public static class PagedResultExtensions
    {
        public static IPagedResult<TTarget> ToProxy<TSource, TTarget>(this IPagedResult<TSource> source, Func<TSource, TTarget> itemConverter) =>
            new ProxyPagedResult<TSource, TTarget>(
                source ?? throw new ArgumentNullException(nameof(source)),
                itemConverter);

        public static async Task<IPagedResult<TTarget>> ToProxy<TSource, TTarget>(this Task<IPagedResult<TSource>> source, Func<TSource, TTarget> itemConverter) =>
            (await source).ToProxy(itemConverter);
    }
}
