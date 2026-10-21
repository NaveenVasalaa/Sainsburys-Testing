using Confluent.Kafka;
using LogisticsPlatform.Testing.Extensions.Helpers;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Utils
{
    public class BaseController<T1, T2> where T1 : class where T2 : class
    {
        private readonly MongoHelper<T1> _mongoHelper;
        private readonly KafkaHelper _kafkaHelper;
        private readonly AzureHelper _azureHelper;
        private readonly KafkaAvroHelper<T2> _kafkaAvroHelper;
        private readonly ApiHelper _apiHelper;
        private readonly MQHelper _mqHelper;
        private readonly OracleHelper _oracleHelper;

        public BaseController(MongoHelper<T1> mongoHelper, KafkaHelper kafkaHelper, AzureHelper azureHelper, KafkaAvroHelper<T2> kafkaAvroHelper, ApiHelper apiHelper, MQHelper mqHelper, OracleHelper oracleHelper)
        {
            _mongoHelper = mongoHelper;
            _kafkaHelper = kafkaHelper;
            _azureHelper = azureHelper;
            _kafkaAvroHelper = kafkaAvroHelper;
            _apiHelper = apiHelper;
            _mqHelper = mqHelper;
            _oracleHelper = oracleHelper;
        }

        public virtual async Task SendMessageToKafkaAsync(string topic, string key, string inputPath, IEnumerable<IHeader> headers = null)
        {
            await _kafkaHelper.SendMessageToKafkaAsync(topic, key, inputPath, headers);
        }

        public virtual async Task SendMessageAsync(string topic, string key, string value, IEnumerable<IHeader> headers = null)
        {
            await _kafkaHelper.SendMessageAsync(topic, key, value, headers);
        }

        public virtual void ConsumeMessageFromKafka(string topic, string consumerGroup, int waitTimeInSeconds, DateTime offSetResetTime, string actualPath, string actualFilePrefix)
        {
            _kafkaHelper.ConsumeMessageFromKafka(topic, consumerGroup, waitTimeInSeconds, offSetResetTime, actualPath, actualFilePrefix);
        }

        public virtual void ConsumeMessageFromKafka(string topic, string consumerGroup, int waitTimeInSeconds, DateTime offSetResetTime, Func<string, string, string> GetKafkaMessage, string actualPath, string actualFilePrefix)
        {
            _kafkaHelper.ConsumeMessageFromKafka(topic, consumerGroup, waitTimeInSeconds, offSetResetTime, GetKafkaMessage, actualPath, actualFilePrefix);
        }

        public virtual async Task SendMessageToKafkaAvroAsync(string topic, string key, T2 value, IEnumerable<IHeader> headers = null)
        {
            await _kafkaAvroHelper.SendMessageToKafkaAsync(topic, key, value, headers);
        }

        public virtual async Task SendMessageToKafkaAvroAsync(string topic, string key, string inputPath, IEnumerable<IHeader> headers = null)
        {
            await _kafkaAvroHelper.SendMessageToKafkaAsync(topic, key, inputPath, headers);
        }

        public virtual void ConsumeAvroMessageFromKafka(string topic, string consumerGroup, int waitTimeInSeconds, DateTime offSetResetTime, string actualPath, string actualFilePrefix)
        {
            _kafkaAvroHelper.ConsumeMessageFromKafka(topic, consumerGroup, waitTimeInSeconds, offSetResetTime, actualPath, actualFilePrefix);
        }

        public virtual void ConsumeAvroMessageFromKafka(string topic, string consumerGroup, int waitTimeInSeconds, DateTime offSetResetTime, Func<string, T2, string> GetKafkaMessage, string actualPath, string actualFilePrefix)
        {
            _kafkaAvroHelper.ConsumeMessageFromKafka(topic, consumerGroup, waitTimeInSeconds, offSetResetTime, GetKafkaMessage, actualPath, actualFilePrefix);
        }

        public virtual async Task DeleteAllDocumentsAsync()
        {
            await _mongoHelper.DeleteAllDocumentsAsync();
        }

        public virtual async Task InsertDocumentsAsync(string inputPath, Func<string, T1> deserializeFunc)
        {
            await _mongoHelper.InsertDocumentsAsync(inputPath, deserializeFunc);
        }

        public virtual async Task InsertDocumentsAsync(T1 inputMessage)
        {
            await _mongoHelper.InsertDocumentsAsync(inputMessage);
        }

        public virtual async Task DeleteAllAzureFilesAsync(string blobPath)
        {
            await _azureHelper.DeleteAllFilesinAzureBlobAsync(blobPath);
        }

        public virtual async Task DownloadAllAzureFilesAsync(string blobPath, string actualPath, List<string> filePrefix, int waitTimeInSeconds, string encryptionKeyVaultName = null, string encryptionKeyName = null)
        {
            await _azureHelper.DownloadFilesFromAzureBlobAsync(blobPath, actualPath, filePrefix, waitTimeInSeconds, encryptionKeyVaultName, encryptionKeyName);
        }

        public virtual async Task UploadAllFilesToAzureAsync(string blobPath, string inputPath)
        {
            await _azureHelper.PutFileInAzureAsync(blobPath, inputPath);
        }

        public virtual async Task UploadAllStreamsToAzureAsync(string blobPath, string inputPath)
        {
            await _azureHelper.PutStreamInAzureAsync(blobPath, inputPath);
        }

        public virtual async Task PostMessageToApiAsync(string apiUrl, string inputPath, string actualPathWithPrefix = "", bool authRequired = true, Dictionary<string, string> headers = null)
        {
            await _apiHelper.PostMessageAsync(apiUrl, inputPath, actualPathWithPrefix, authRequired, headers);
        }

        public virtual async Task GetMessageFromApiAsync(string apiUrl, string actualPath, string outputFilePrefix = "", bool authRequired = true, Dictionary<string, string> headers = null)
        {
            await _apiHelper.GetMessageAsync(apiUrl, actualPath, outputFilePrefix, authRequired, headers);
        }

        public virtual void PostMessagesToMQ(string inputPath, string inputFilePrefix = null)
        {
            _mqHelper.PostMessagesToMQ(inputPath, inputFilePrefix);
        }

        public virtual async Task InserDataInOracleDbAsync(string tableName, Dictionary<string, object> fields)
        {
            await _oracleHelper.InsertDataAsync(tableName, fields);
        }

        public virtual async Task ReadDataFromOracleDbAsync(string query, string actualPath, string actualFilePrefix)
        {
            await _oracleHelper.ReadDataAndStoreInFileAsync(query, actualPath, actualFilePrefix);
        }

        public virtual bool VerifyNoOutput(string actualFilesPath, string filePrefix, string logFilePath, StringBuilder errorMessage = null)
        {
            var actualFiles = FileHelper.GetAllFiles(actualFilesPath, filePrefix + "*");

            if (actualFiles.Length > 0)
            {
                FileHelper.AppendLine(logFilePath, $"Actual files generated in {actualFilesPath}");
                errorMessage?.AppendLine($"Actual files generated in {actualFilesPath}");
                return false;
            }

            FileHelper.AppendLine(logFilePath, "No files generated");
            FileHelper.AppendLine(logFilePath, new string('-', 100));

            return true;
        }
    }
}
