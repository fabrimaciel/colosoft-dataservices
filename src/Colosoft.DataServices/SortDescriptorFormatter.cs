using System.Collections.Generic;
using System.Linq;

namespace Colosoft.DataServices
{
    internal static class SortDescriptorFormatter
    {
        public static string Format(IEnumerable<SortDescriptor> sorts) =>
            string.Join(",", sorts.Select(f =>
            {
                if (f.SortOrder == SortOrder.Descending)
                {
                    return $"{f.Property}:desc";
                }

                return f.Property;
            }));
    }
}
