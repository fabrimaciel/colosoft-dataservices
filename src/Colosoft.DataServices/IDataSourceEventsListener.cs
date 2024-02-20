namespace Colosoft.DataServices
{
    public interface IDataSourceEventsListener
    {
        void Add(IDataSourceObserver observer);

        bool Remove(IDataSourceObserver observer);
    }
}
