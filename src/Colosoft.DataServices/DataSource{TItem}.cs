using System;
using System.Collections;
using System.Collections.Generic;

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

        public DataSource(
            IEnumerable<TItem> items)
            : base(items ?? throw new ArgumentNullException(nameof(items)), f => (TItem)f)
        {
        }
    }
}
