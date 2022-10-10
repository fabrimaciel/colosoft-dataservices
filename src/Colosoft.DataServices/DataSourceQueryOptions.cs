using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public class DataSourceQueryOptions
    {
        public DataSourceQueryOptions(
            IEnumerable<SortDescriptor> sorts,
            int page,
            int pageSize)
        {
            this.Sorts = sorts;
            this.Page = page;
            this.PageSize = pageSize;
        }

        public IEnumerable<SortDescriptor> Sorts { get; }

        public int Page { get; }

        public int PageSize { get; set; }

        public IList<IDataSourceFilterItem> Filters { get; } = new List<IDataSourceFilterItem>();

        public object? FilterModel { get; set; }
    }
}