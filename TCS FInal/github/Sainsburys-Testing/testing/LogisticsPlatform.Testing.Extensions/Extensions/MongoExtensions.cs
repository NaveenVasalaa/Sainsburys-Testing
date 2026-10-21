using LogisticsPlatform.Common.Extensions.Mongo.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace LogisticsPlatform.Testing.Extensions.Extensions;

public static class MongoExtensions
{
    public static void SetConventions()
    {
        ConventionRegistry.Register("Conventions", new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(ignoreExtraElements: true),
            new IgnoreIfNullConvention(ignoreIfNull: true)
        }, (Type filter) => true);
        BsonSerializer.RegisterSerializer(new DateTimeOffsetBsonSerializer());
    }
}
