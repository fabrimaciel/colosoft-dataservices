using Refit;

namespace Colosoft.DataServices.Refit
{
    internal interface ISettingsFor
    {
        RefitSettings? Settings { get; }
    }
}