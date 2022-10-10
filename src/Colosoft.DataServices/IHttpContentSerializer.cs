using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IHttpContentSerializer
    {
         Task<T> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken);
    }
}