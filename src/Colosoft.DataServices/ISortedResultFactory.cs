using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface ISortedResultFactory
    {
        Task<ISortedResult<T>> Create<T>(System.Net.Http.HttpResponseMessage response, CancellationToken cancellationToken);

        Task<ISortedResult<T>> Create<T>(System.Uri address, CancellationToken cancellationToken);
    }
}