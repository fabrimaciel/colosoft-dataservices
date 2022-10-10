using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public interface IDataSource<out TItem> : IDataSource, IEnumerable<TItem>
    {
        IEnumerable<TItem> Data { get; }
    }
}