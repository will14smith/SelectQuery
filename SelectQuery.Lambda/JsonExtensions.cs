using System.Text.Json;

namespace SelectQuery.Lambda
{
    public static class JsonExtensions
    {
        public static TValue FastReadObject<TValue>(this ref Utf8JsonReader reader) where TValue : class
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() as TValue,
                JsonTokenType.Number => reader.GetDecimal() as TValue,
                JsonTokenType.True => reader.GetBoolean() as TValue,
                JsonTokenType.False => reader.GetBoolean() as TValue,
                JsonTokenType.Null => null,
                _ => JsonSerializer.Deserialize<TValue>(ref reader)
            };
        }
    }
}