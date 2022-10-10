namespace Colosoft.DataServices
{
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface IDataSourceQuery : IDataSource
#pragma warning restore CA1010 // Collections should implement generic interface
    {
        event DataSourceQueryOptionsCreatedHandler DataSourceQueryOptionsCreated;
    }
}
