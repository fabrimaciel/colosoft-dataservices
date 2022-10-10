namespace Colosoft.DataServices.Refit
{
    public interface IRefitServiceFactory
    {
        T Create<T>();
    }
}