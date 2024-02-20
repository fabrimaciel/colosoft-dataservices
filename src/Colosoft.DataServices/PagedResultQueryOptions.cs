namespace Colosoft.DataServices
{
    public class PagedResultQueryOptions
    {
        public PagedResultQueryOptions(int page, int pageSize)
        {
            this.Page = page;
            this.PageSize = pageSize;
        }

        public int Page { get; }

        public int PageSize { get; }
    }
}