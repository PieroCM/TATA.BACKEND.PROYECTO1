using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// Converter personalizado para manejar DateTime nullable que puede venir
    /// como null, string vacío o una fecha válida desde el JSON.
    /// Esto evita errores 400 durante la deserialización cuando FechaIngreso está vacía.
    /// </summary>
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Si el token es null o undefined
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // Si es un string
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                
                // Si es string vacío, devolver null
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                // Intentar parsear la fecha
                if (DateTime.TryParse(stringValue, out var date))
                {
                    return date;
                }

                // Si no se puede parsear, devolver null en lugar de lanzar excepción
                return null;
            }

            // Si no es string ni null, intentar leer como DateTime directamente
            try
            {
                return reader.GetDateTime();
            }
            catch
            {
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
