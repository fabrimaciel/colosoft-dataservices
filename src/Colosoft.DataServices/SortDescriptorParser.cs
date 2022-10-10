using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosoft.DataServices
{
    public static class SortDescriptorParser
    {
        public static IEnumerable<SortDescriptor> Parse(Uri address)
        {
            if (!string.IsNullOrEmpty(address.Query))
            {
                var parts = System.Web.HttpUtility.ParseQueryString(address.Query);
                var sortExpression = parts[SortingConstants.SortingParameterName];

                if (!string.IsNullOrEmpty(sortExpression))
                {
                    return System.Web.HttpUtility.UrlDecode(sortExpression)
                        .Split(",")
                        .Select(f =>
                        {
                            var segments = f.Split(":");
                            if (segments.Length > 1)
                            {
                                return new SortDescriptor
                                {
                                    Property = segments[0],
                                    SortOrder = StringComparer.InvariantCultureIgnoreCase.Equals(segments[1], "desc") ? SortOrder.Descending : SortOrder.Ascending,
                                };
                            }
                            else
                            {
                                return new SortDescriptor
                                {
                                    Property = segments[0],
                                };
                            }
                        })
                        .ToList();
                }
            }

            return Array.Empty<SortDescriptor>();
        }
    }
}
