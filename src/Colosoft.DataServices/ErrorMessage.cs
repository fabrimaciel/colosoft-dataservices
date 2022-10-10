using System.Text.Json.Serialization;

namespace Colosoft.DataServices
{
    public class ErrorMessage
    {
        [JsonPropertyName("codigo")]
        public int Code { get; set; }

        [JsonPropertyName("mensagem")]
        public string? Message { get; set; }

        [JsonPropertyName("pilha")]
        public string? StackTrace { get; set; }
    }
}
