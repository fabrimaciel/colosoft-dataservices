using Refit;

namespace Colosoft.DataServices.Refit
{
    internal class SettingsFor<T> : ISettingsFor
    {
        public SettingsFor(RefitSettings? settings) => this.Settings = settings;

        public RefitSettings? Settings { get; }
    }
}