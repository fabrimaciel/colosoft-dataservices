using Refit;

namespace Colosoft.DataServices
{
    public class Paging
    {
        public Paging(int page, int pageSize)
        {
            this.Page = page;
            this.PageSize = pageSize;
        }

        public Paging(IPagedQueryInput input)
        {
            this.Page = input.Page;
            this.PageSize = input.PageSize;
        }

        [AliasAs(PagingConstants.PageName)]
        public int Page { get; set; }

        [AliasAs(PagingConstants.PageSizeName)]
        public int PageSize { get; set; }
    }
}
