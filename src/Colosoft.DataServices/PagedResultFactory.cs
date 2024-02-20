using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class PagedResultFactory : IPagedResultFactory
    {
        public async Task<IPagedResult<T>> Create<T>(
            PagedResultQueryHandler<T> handler,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var content = await handler.Invoke(new PagedResultQueryOptions(page, pageSize), cancellationToken);

            return new PagedResultQuery<T>(content, handler, page, pageSize);
        }
    }
}
