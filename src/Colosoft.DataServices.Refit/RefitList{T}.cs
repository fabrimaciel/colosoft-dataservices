using System.Collections;
using System.Collections.Generic;

namespace Colosoft.DataServices.Refit
{
    [System.Text.Json.Serialization.JsonConverter(typeof(RefitListJsonConverter))]
    public class RefitList<T> : IEnumerable<T>
    {
        public RefitList(RefitListLinks links, long total, IEnumerable<T> items)
        {
            this.Links = links;
            this.Total = total;
            this.Items = items;
        }

        public RefitListLinks Links { get; }

        public long Total { get; }

        internal IEnumerable<T> Items { get; }

        public IEnumerator GetEnumerator() => this.Items.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.Items.GetEnumerator();
    }
}
