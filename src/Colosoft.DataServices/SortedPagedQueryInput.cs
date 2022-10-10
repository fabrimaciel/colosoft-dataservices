using System;

namespace Colosoft.DataServices
{
    public abstract class SortedPagedQueryInput : PagedQueryInput, ISortedQueryInput
    {
        private string? sorting;

        public virtual string? Sorting => this.sorting;

        public void Apply(ISortedQueryInput input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            this.sorting = input.Sorting;
        }

        public void ApplySorting(string sorting)
        {
            this.sorting = sorting;
        }
    }
}
