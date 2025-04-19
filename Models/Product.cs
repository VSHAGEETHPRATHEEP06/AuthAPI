using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace AuthApi.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("UserId")]
        public Guid UserId { get; set; }
    }
}