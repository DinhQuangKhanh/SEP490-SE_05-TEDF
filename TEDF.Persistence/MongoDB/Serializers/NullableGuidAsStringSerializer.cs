using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace TEDF.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// Custom serializer for nullable Guid.
    /// </summary>
    public class NullableGuidAsStringSerializer : SerializerBase<Guid?>
    {
        public override Guid? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.GetCurrentBsonType();
            if (type == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            return type switch
            {
                BsonType.String => Guid.Parse(context.Reader.ReadString()),
                BsonType.Binary => context.Reader.ReadBinaryData().ToGuid(),
                _ => throw new BsonSerializationException($"Cannot deserialize Guid? from BsonType {type}")
            };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Guid? value)
        {
            if (value.HasValue)
                context.Writer.WriteString(value.Value.ToString());
            else
                context.Writer.WriteNull();
        }
    }
}
