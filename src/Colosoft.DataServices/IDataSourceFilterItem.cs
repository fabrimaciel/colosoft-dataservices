using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public interface IDataSourceFilterItem
    {
        IEnumerable<DataSourceFilterKeyValueAction> GetFilterConditions();
    }
}
