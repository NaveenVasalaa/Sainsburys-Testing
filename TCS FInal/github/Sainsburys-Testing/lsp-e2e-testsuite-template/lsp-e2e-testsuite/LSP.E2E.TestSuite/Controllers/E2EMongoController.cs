using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LSP.E2E.TestSuite.Controllers
{
    public class E2EMongoController : BaseController<BsonDocument, E2ESuiteRunConfig>
    {

        private readonly MongoHelper<BsonDocument> _mongoHelper;

        public E2EMongoController(MongoHelper<BsonDocument> mongoHelper) : base(mongoHelper, null, null, null, null, null, null)
        {

            _mongoHelper = mongoHelper;
        }

        public async Task<E2ESuiteRunConfig> DownloadE2EMongoDataAsync(string Service)
        {
            E2ESuiteRunConfig e2ESuiteRunConfig = new E2ESuiteRunConfig();

            var filterCollection = Builders<BsonDocument>.Filter.Eq("service", Service);
            var messages = await _mongoHelper.GetAllAsync(filterCollection);
            foreach (var message in messages)
            {
                e2ESuiteRunConfig = BsonSerializer.Deserialize<E2ESuiteRunConfig>(message.ToBsonDocument());
            }
            return e2ESuiteRunConfig;
        }
    }
}
