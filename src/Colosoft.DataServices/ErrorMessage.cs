using System.Collections.Generic;

namespace Colosoft.DataServices
{
    public class ErrorMessage
    {
        public int Code { get; set; }

        public string Type { get; set; }

        public string? Message { get; set; }

        public string? StackTrace { get; set; }

        public ErrorMessage? Inner { get; set; }

        public IDictionary<string, string> Metadata { get; set; }
    }
}
