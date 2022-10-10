using System;

namespace Refit
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class RefitServiceAttribute : Attribute
    {
        public string? HttpClientName { get; set; }

        public RefitServiceAttribute(string? httpClientName = null)
        {
            this.HttpClientName = httpClientName;
        }
    }
}