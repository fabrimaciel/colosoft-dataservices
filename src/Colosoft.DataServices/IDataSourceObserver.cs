using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IDataSourceObserver
    {
        Task OnRefreshed(CancellationToken cancellationToken);

        Task OnReseted(CancellationToken cancellationToken);

        Task OnDataChanged(CancellationToken cancellationToken);
    }
}
