using Refit;

namespace Colosoft.DataServices.Refit
{
    public class RefitServiceSettings : RefitSettings
    {
        public string? HttpClientName { get; set; }
    }
}