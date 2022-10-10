using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IResettableResult
    {
        Task<IResettableResult?> Reset(CancellationToken cancellationToken);
    }
}
