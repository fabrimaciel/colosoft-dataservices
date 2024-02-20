using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IHttpSortedPagedResultFactory
    {
        ISortedPagedResult<T> Create<T>(System.Net.Http.HttpResponseMessage response, System.Collections.Generic.IEnumerable<T> items);

        Task<ISortedPagedResult<T>> Create<T>(System.Net.Http.HttpResponseMessage response, CancellationToken cancellationToken);

        Task<ISortedPagedResult<T>> Create<T>(System.Uri address, CancellationToken cancellationToken);
    }
}