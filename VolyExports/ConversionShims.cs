using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VolyExports
{
    /// <summary>
    /// A shim for Microsoft's lackluster TimeSpan support using the new json serializer.
    /// Mark up properties with "[JsonConverter(typeof(TimeSpanConverter))]".
    /// </summary>
    internal class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new NotSupportedException();
            }

            return TimeSpan.ParseExact(reader.GetString(), "c", System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
