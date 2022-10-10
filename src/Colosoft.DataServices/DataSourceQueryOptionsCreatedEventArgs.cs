using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public delegate Task DataSourceQueryOptionsCreatedHandler(object sender, DataSourceQueryOptionsCreatedEventArgs e, CancellationToken cancellationToken);

    public class DataSourceQueryOptionsCreatedEventArgs
    {
        public DataSourceQueryOptionsCreatedEventArgs(DataSourceQueryOptions options)
        {
            this.Options = options;
        }

        public DataSourceQueryOptions Options { get; }
    }
}