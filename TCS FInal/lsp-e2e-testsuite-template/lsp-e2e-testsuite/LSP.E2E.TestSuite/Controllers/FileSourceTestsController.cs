using MongoDB.Bson;

namespace LSP.E2E.TestSuite.Controllers;

//TODO: Update the ModelJson with the model to be used for MongoDB and Kafka respectively
public class FileSourceTestsController : BaseController<ModelJson, ModelJson>
{
    private readonly FileGenerationHelper _fileGenerationHelper;
    private readonly MongoHelper<ModelJson> _mongoHelper;

    public FileSourceTestsController(MongoHelper<ModelJson> mongoHelper, KafkaHelper kafkaHelper, KafkaAvroHelper<ModelJson> kafkaAvroHelper, AzureHelper azureHelper, MQHelper mQHelper) : base(mongoHelper, kafkaHelper, azureHelper, kafkaAvroHelper, null, mQHelper, null)
    {
        _fileGenerationHelper = new FileGenerationHelper(AppConstants.FileSource_TestDataFile, AppConstants.LogFile);
        _mongoHelper = mongoHelper;
    }

    public void GenerateInputFiles(string scenario, string workSheetName, string inputPath)
    {
        // TODO: Read input data from Excel and generate Flat files
        StringBuilder stringBuilder = new();

        _fileGenerationHelper.GenerateFlatFiles(scenario, workSheetName,
            fields =>
            {
                return stringBuilder;
            }, inputPath, AppConstants.InputFilePrefix, AppConstants.FileSourceInputFolder, "<extension>");
    }

    public void GenerateExpectedInboundKafkaFiles(string scenario, string workSheetName, string expectedPath)
    {
        //TODO: Read expected inbound data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
           fields =>
           {
               var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));
               return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
           }, expectedPath, AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedInboundFolder);
    }

    public void GenerateExpectedOutboundKafkaFiles(string scenario, string workSheetName, string expectedPath)
    {
        //TODO: Read expected outbound data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
           fields =>
           {
               var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));
               return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
           }, expectedPath, AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedOutboundFolder);
    }

    public async Task DownloadMongoEntriesAsync(string outputPath, string filePrefix)
    {
        //TODO: Update the ModelJson with the model to be used for MongoDB
        var projection = Builders<ModelJson>.Projection.Exclude(x => x.Model);
        var findOptions = new FindOptions<ModelJson, BsonDocument>
        {
            Projection = projection
        };

        await _mongoHelper.GetAllDocumentsAsync(outputPath, filePrefix, findOptions);
    }

    public void GenerateExpectedAdaptorOutputFiles(string scenario, string workSheetName, string expectedPath)
    {
        StringBuilder stringBuilder = new();

        _fileGenerationHelper.GenerateFlatFiles(scenario, workSheetName,
            fields =>
            {
                stringBuilder.AppendLine(fields[1]);
                return stringBuilder;
            }, expectedPath, AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedAdaptorFolder, ".txt");
    }


    public static void ComparerConfigure()
    {
        // TODO: Ignore properties that are not required to be compared
        Comparer.Configure<TestModel>()
        //.Ignore(x => x.CreateDateTime)
        .Compile.Now();
    }

    public static bool CompareOutputFiles(string expectedFilesPath, string actualFilesPath, string filePrefix, string comparisonFiles)
    {
        var helper = new FileComparisonHelper(expectedFilesPath, actualFilesPath, filePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage);
        return helper.CompareJsonFiles<ModelJson>(
            comparisonFiles,
            keySelector: x => x.Model.Uuid);
    }
}