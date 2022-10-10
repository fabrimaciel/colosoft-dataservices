using Refit;
using System;

namespace Colosoft.DataServices
{
    public class Sorting
    {
        public Sorting(string? value)
        {
            this.Value = value;
        }

        public Sorting(ISortedQueryInput input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            this.Value = input.Sorting;
        }

        [AliasAs(SortingConstants.SortingParameterName)]
        public string? Value { get; }
    }
}
