using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace TEDF.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// Custom serializer for Guid to store as string in MongoDB.
    /// </summary>
    public class GuidAsStringSerializer : SerializerBase<Guid>
    {
        public override Guid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.GetCurrentBsonType();
            return type switch
            {
                BsonType.String => Guid.Parse(context.Reader.ReadString()),
                BsonType.Binary => context.Reader.ReadBinaryData().ToGuid(),
                _ => throw new BsonSerializationException($"Cannot deserialize Guid from BsonType {type}")
            };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Guid value)
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}
