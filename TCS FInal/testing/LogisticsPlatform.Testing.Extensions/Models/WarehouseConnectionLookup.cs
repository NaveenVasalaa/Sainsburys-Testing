using MongoDB.Bson;

namespace LogisticsPlatform.Testing.Extensions.Models
{
    public class WarehouseConnectionLookup
    {
        public ObjectId Id { get; set; }
        public string DataBase { get; set; }
        public string DepotNumber { get; set; }
        public string PlaceId { get; set; }
        public bool IsEnabled { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public string UserName { get; set; }
        public string KeyVaultName { get; set; }
        public string ClusterId { get; set; }
        public string SchemaName { get; set; }
    }
}
