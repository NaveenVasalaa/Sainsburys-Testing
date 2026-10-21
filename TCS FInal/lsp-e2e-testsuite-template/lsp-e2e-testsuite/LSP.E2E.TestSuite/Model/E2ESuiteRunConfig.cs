using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LSP.E2E.TestSuite.Model
{
    //TODO: Update the properties of E2ESuiteRunConfig class based on the configuration values that you want to fetch from MongoDB for your E2ETestSuite and Collection. You can also add additional properties as needed.
    public class E2ESuiteRunConfig
    {
        [JsonPropertyName("_id")]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [JsonPropertyName("environment")]
        [BsonElement("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("service")]
        [BsonElement("service")]
        public string Service { get; set; }

        [JsonPropertyName("suiteType")]
        [BsonElement("suiteType")]
        public string SuiteType { get; set; }

         [JsonPropertyName("emailReportEnabled")]
        [BsonElement("emailReportEnabled")]
        public bool EmailReportEnabled { get; set; }

        [JsonPropertyName("testReportMailRecipients")]
        [BsonElement("testReportMailRecipients")]
        public List<string> TestReportMailRecipients { get; set; }



        //TODO: Other configuration properties can be added here as needed for the test suite run like toggles for different components, execution parameters like below.
        [JsonPropertyName("apiSourceComponentNameEnabled")]
        [BsonElement("apiSourceComponentNameEnabled")]
        public bool ApiSourceComponentNameEnabled { get; set; }

        [JsonPropertyName("fileSourceComponentNameEnabled")]
        [BsonElement("fileSourceComponentNameEnabled")]
        public bool FileSourceComponentNameEnabled { get; set; }

        [JsonPropertyName("kafkaSourceComponentNameEnabled")]
        [BsonElement("kafkaSourceComponentNameEnabled")]
        public bool KafkaSourceComponentNameEnabled { get; set; }
    }
}
