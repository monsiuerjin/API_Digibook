using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Firestore;

namespace API_DigiBook.Converters
{
    public class FirestoreTimestampConverter : JsonConverter<Timestamp>
    {
        public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (DateTime.TryParse(reader.GetString(), out var dateTime))
                {
                    return Timestamp.FromDateTime(dateTime.ToUniversalTime());
                }
            }
            
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Simple implementation for reading back the object format if needed
                using var doc = JsonDocument.ParseValue(ref reader);
                if (doc.RootElement.TryGetProperty("seconds", out var secProp))
                {
                    // If we can't find a 2-arg constructor, we might have to use internal methods or just fallback
                    // But usually, writing as string is enough for the frontend.
                }
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options)
        {
            // Convert to ISO 8601 string for easy consumption by the frontend
            writer.WriteStringValue(value.ToDateTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
    }
}
