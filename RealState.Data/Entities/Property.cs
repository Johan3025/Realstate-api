using MongoDB.Bson;

namespace RealState.Data.Entities
{
    using MongoDB.Bson.Serialization.Attributes;

    [BsonIgnoreExtraElements]
    public class Property
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;

        [BsonElement("Name")] public string Name { get; set; } = default!;
        [BsonElement("Address")] public string Address { get; set; } = default!;
        [BsonElement("Price"), BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
        [BsonElement("CodeInternal")] public string CodeInternal { get; set; } = default!;
        [BsonElement("Year")] public int Year { get; set; }

        [BsonElement("Owner")] public Owner Owner { get; set; } = new();

        [BsonElement("Image")] public Image Image { get; set; } = new();
        [BsonElement("Traces")] public List<PropertyTrace> Traces { get; set; } = new();
    }

    [BsonIgnoreExtraElements]
    public class Owner
    {
        // ⚠️ Importante: NO BsonId, NO BsonRepresentation aquí.
        [BsonElement("Id")] public string Id { get; set; } = string.Empty;
        [BsonElement("Name")] public string Name { get; set; } = string.Empty;
        [BsonElement("Address")] public string Address { get; set; } = string.Empty;
        [BsonElement("Photo")] public string Photo { get; set; } = string.Empty;
        [BsonElement("Birthday")] public DateTime Birthday { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Image
    {
        [BsonElement("File")] public string File { get; set; } = string.Empty;
        [BsonElement("Description")] public string Description { get; set; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    public class PropertyTrace
    {
        [BsonElement("DateSale")] public DateTime DateSale { get; set; }
        [BsonElement("Name")] public string Name { get; set; } = string.Empty;
        [BsonElement("Value"), BsonRepresentation(BsonType.Decimal128)]
        public decimal Value { get; set; }
        [BsonElement("Tax"), BsonRepresentation(BsonType.Decimal128)]
        public decimal Tax { get; set; }
    }

}