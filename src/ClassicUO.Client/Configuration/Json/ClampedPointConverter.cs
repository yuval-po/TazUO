using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class ClampedPointConverterAttribute(int minX = -50, int minY = -50, int maxX = int.MaxValue, int maxY = int.MaxValue)
    : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert) => new ClampedPointConverter(minX, minY, maxX, maxY);
}

internal class ClampedPointConverter : JsonConverter<Point>
{
    private readonly int _minX;
    private readonly int _minY;
    private readonly int _maxX = int.MaxValue;
    private readonly int _maxY = int.MaxValue;

    public ClampedPointConverter()
    {
    }

    public ClampedPointConverter(int minX = -50, int minY = -50, int maxX = int.MaxValue, int maxY = int.MaxValue)
    {
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
    }

    public sealed override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return Point.Zero;

        var point = new Point { X = reader.GetInt32() };

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return Point.Zero;

        point.Y = reader.GetInt32();

        reader.Read();

        return reader.TokenType != JsonTokenType.EndObject
            ? Point.Zero
            : point;
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", Math.Clamp(value.X, _minX, _maxX));
        writer.WriteNumber("Y", Math.Clamp(value.Y, _minY, _maxY));
        writer.WriteEndObject();
    }
}
