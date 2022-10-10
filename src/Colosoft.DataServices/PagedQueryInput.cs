namespace Colosoft.DataServices
{
    public abstract class PagedQueryInput : IPagedQueryInput
    {
        protected PagedQueryInput(int pageSize = int.MaxValue)
        {
            this.PageSize = pageSize;
        }

        public virtual int Page { get; protected set; } = 1;

        public virtual int PageSize { get; protected set; }

        public void Apply(IPagedQueryInput input)
        {
            this.Page = input.Page;
            this.PageSize = input.PageSize;
        }

        public void ApplyPaging(int page, int pageSize)
        {
            this.Page = page;
            this.PageSize = pageSize;
        }
    }
}
