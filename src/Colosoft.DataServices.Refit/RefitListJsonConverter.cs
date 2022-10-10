using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Colosoft.DataServices.Refit
{
    public class RefitListJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IPagedResult<>);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (this.CanConvert(typeToConvert))
            {
                var wrapperType = typeof(Wrapper<>).MakeGenericType(typeToConvert.GetGenericArguments());
                var wrapper = (IWrapper?)JsonSerializer.Deserialize(ref reader, wrapperType, options);

                return wrapper?.ToRefitList();
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        private sealed class Wrapper<T> : IWrapper
        {
            [JsonPropertyName("_links")]
            public RefitListLinks Links { get; set; } = new RefitListLinks();

            public long Total { get; set; }

            public IEnumerable<T> Results { get; set; } = Array.Empty<T>();

            public object ToRefitList() =>
                new RefitList<T>(this.Links!, this.Total, this.Results);
        }

        private interface IWrapper
        {
            object ToRefitList();

            long Total { get; set; }
        }
    }
}