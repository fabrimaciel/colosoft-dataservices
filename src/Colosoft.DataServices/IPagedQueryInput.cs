namespace Colosoft.DataServices
{
    public interface IPagedQueryInput
    {
        int Page { get; }

        int PageSize { get; }
    }
}
