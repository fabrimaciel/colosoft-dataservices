using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IPagedResultFactory
    {
        Task<IPagedResult<T>> Create<T>(System.Net.Http.HttpResponseMessage response, CancellationToken cancellationToken);

        Task<IPagedResult<T>> Create<T>(System.Uri address, CancellationToken cancellationToken);
    }
}