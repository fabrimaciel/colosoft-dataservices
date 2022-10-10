namespace Colosoft.DataServices
{
    public interface IDataSource<TFilter, out TItem> : IDataSource<TItem>
    {
        TFilter Filter { get; set; }
    }
}