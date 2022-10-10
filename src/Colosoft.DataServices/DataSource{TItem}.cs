using System;
using System.Collections;

namespace Colosoft.DataServices
{
    public class DataSource<TItem> : DataSourceBase<TItem>
    {
        public DataSource(
            IEnumerable items,
            Func<object, TItem> itemConverter)
            : base(items ?? throw new ArgumentNullException(nameof(items)), itemConverter)
        {
        }
    }
}
