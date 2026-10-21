using LogisticsPlatform.Common.Extensions;
using LogisticsPlatform.Common.Extensions.Mongo;
using LogisticsPlatform.Common.Extensions.Mongo.Settings;
using LogisticsPlatform.Testing.Extensions.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class MongoHelper<T> : BaseRepository<T> where T : class
{
    private readonly string logFilePath;
    public MongoHelper(string connectionString, string databaseName, string collectionName, string logFilePath)
        : base(new()
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName,
            CollectionName = collectionName,
            MongoClientSettings = GetClientSettings(connectionString)
        })
    {        
        this.logFilePath = logFilePath;
    }

    public MongoHelper(MongoDbSettingsBase mongoDbSettings) : base(mongoDbSettings)
    {        
    }

    private static MongoClientSettings GetClientSettings(string connectionString)
    {
        var mongoUrl = new MongoUrl(connectionString);
        var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);

        if (mongoUrl.AuthenticationMechanism == "MONGODB-OIDC"
        && mongoUrl.AuthenticationMechanismProperties.FirstOrDefault(a => a.Key == "ENVIRONMENT").Value == "azure")
        {
            mongoClientSettings.Credential = MongoCredential
                .CreateOidcCredential(new MongoOidcCallback(mongoUrl.Username,
                    mongoUrl.AuthenticationMechanismProperties.FirstOrDefault(a => a.Key == "TOKEN_RESOURCE").Value));
        }

        mongoClientSettings.MaxConnectionIdleTime = new TimeSpan(0, 0, 59);
        mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);

        return mongoClientSettings;
    }

    public async Task DeleteAllDocumentsAsync()
    {
        FileHelper.AppendLine(logFilePath, $"Deleting Documents in DB");

        var filterBuilder = Builders<T>.Filter.Empty;
        await DeleteAllAsync(filterBuilder);

        FileHelper.AppendLine(logFilePath, $"Documents deleted in DB");
    }

    public async Task DeleteAllDocumentsAsync(FilterDefinition<T> filter)
    {
        FileHelper.AppendLine(logFilePath, $"Deleting Documents in DB");

        await DeleteAllAsync(filter);

        FileHelper.AppendLine(logFilePath, $"Documents deleted in DB");
    }

    public async Task GetAllDocumentsAsync(string actualPath, string outputFilePrefix)
    {
        FileHelper.AppendLine(logFilePath, "Started Downloading Documents");
        var messages = await GetAllAsync();
        int count = 1;
        foreach (var message in messages)
        {
            FileHelper.WriteLine(actualPath + "/" + outputFilePrefix + count + ".json", message.ToJsonString());
            FileHelper.AppendLine(logFilePath, $"Downloaded {count} Document");
            count++;
        }
    }

    public async Task GetAllDocumentsAsync(string actualPath, string outputFilePrefix, FindOptions<T, BsonDocument> findOptions)
    {
        FileHelper.AppendLine(logFilePath, "Started Downloading Documents");
        var messages = await GetAllAsync(findOptions);
        int count = 1;
        foreach (var message in messages)
        {
            var jsonMessage = BsonTypeMapper.MapToDotNetValue(message);
            FileHelper.WriteLine(actualPath + "/" + outputFilePrefix + count + ".json", jsonMessage.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions));
            FileHelper.AppendLine(logFilePath, $"{count} Downloaded Document");
            count++;
        }
    }

    public async Task GetAllDocumentsAsync(string actualPath, string outputFilePrefix, FilterDefinition<T> filter, FindOptions<T, BsonDocument> findOptions)
    {
        FileHelper.AppendLine(logFilePath, "Started Downloading Documents");
        var messages = await GetAllAsync(filter, findOptions);
        int count = 1;
        foreach (var message in messages)
        {
            var jsonMessage = BsonTypeMapper.MapToDotNetValue(message);
            FileHelper.WriteLine(actualPath + "/" + outputFilePrefix + count + ".json", jsonMessage.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions));
            FileHelper.AppendLine(logFilePath, $"{count} Downloaded Document");
            count++;
        }
    }

    public async Task GetDocumentAsync(string actualPath, string outputFilePrefix, FilterDefinition<T> filter)
    {
        FileHelper.AppendLine(logFilePath, "Started Downloading Document");
        var message = await GetAsync(filter);
        FileHelper.WriteLine(actualPath + "/" + outputFilePrefix + ".json", message.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions));
        FileHelper.AppendLine(logFilePath, $"Downloaded Document");
    }

    public async Task GetDocumentAsync(string actualPath, string outputFilePrefix, FilterDefinition<T> filter, FindOptions<T, BsonDocument> findOptions)
    {
        FileHelper.AppendLine(logFilePath, "Started Downloading Document");
        var message = await GetAsync(filter, findOptions);
        var jsonMessage = BsonTypeMapper.MapToDotNetValue(message);
        FileHelper.WriteLine(actualPath + "/" + outputFilePrefix + ".json", jsonMessage.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions));
        FileHelper.AppendLine(logFilePath, $"Downloaded Document");        
    }

    public virtual async Task InsertDocumentsAsync(T inputMessage)
    {
        FileHelper.AppendLine(logFilePath, "Inserting Document in DB");
        await InsertAsync(inputMessage);
        FileHelper.AppendLine(logFilePath, "Document inserted in DB");
    }

    public virtual async Task InsertDocumentsAsync(string inputPath, Func<string, T> deserializeFunc)
    {
        var inputFiles = FileHelper.GetAllFiles(inputPath);
        foreach (var inputFile in inputFiles)
        {
            var json = FileHelper.GetAllText(inputFile);
            T message = deserializeFunc(json);
            await InsertDocumentsAsync(message);
        }
    }
}
