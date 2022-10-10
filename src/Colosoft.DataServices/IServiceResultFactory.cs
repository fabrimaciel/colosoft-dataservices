using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IServiceResultFactory
    {
        Task<IEnumerable<T>> Create<T>(System.Net.Http.HttpResponseMessage response, CancellationToken cancellationToken);

        Task<IEnumerable<T>> Create<T>(System.Uri address, CancellationToken cancellationToken);
    }
}