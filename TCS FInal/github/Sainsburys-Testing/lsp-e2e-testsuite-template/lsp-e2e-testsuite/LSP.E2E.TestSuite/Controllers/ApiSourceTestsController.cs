using MongoDB.Bson;

namespace LSP.E2E.TestSuite.Controllers;

//TODO: Update the ModelJson with the model to be used for MongoDB and Kafka respectively
public class ApiSourceTestsController : BaseController<ModelJson, ModelJson>
{
    private readonly FileGenerationHelper _fileGenerationHelper;
    private readonly MongoHelper<ModelJson> _mongoHelper;

    public ApiSourceTestsController(MongoHelper<ModelJson> mongoHelper, KafkaHelper kafkaHelper, KafkaAvroHelper<ModelJson> kafkaAvroHelper, ApiHelper apiHelper)
        : base(mongoHelper, kafkaHelper, null, kafkaAvroHelper, apiHelper, null, null)
    {
        _fileGenerationHelper = new FileGenerationHelper(AppConstants.ApiSource_TestDataFile, AppConstants.LogFile);
        _mongoHelper = mongoHelper;
    }

    public void GenerateInputFiles(string scenario, string workSheetName, string inputFolder)
    {
        // TODO: Read input data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
        fields =>
        {
            var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));
            return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
        }, inputFolder, AppConstants.InputFilePrefix, AppConstants.ApiSourceInputFolder);
    }

    public void GenerateExpectedInboundKafkaFiles(string scenario, string workSheetName, string expectedFolder)
    {
        //TODO: Read expected inbound data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
        fields =>
        {
            var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));

            return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
        }, expectedFolder, AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedInboundFolder);
    }

    public void GenerateExpectedOutboundKafkaFiles(string scenario, string workSheetName, string expectedFolder)
    {
        //TODO: Read expected outbound data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
        fields =>
        {
            var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));
            return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
        }, expectedFolder, AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedOutboundFolder);
    }

    public void GenerateExpectedOutputErrorFiles(string scenario, string workSheetName, string expectedFolder)
    {
        //TODO: Read expected error data from Excel and generate JSON files using model
        _fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName,
        fields =>
        {
            var model = new ModelJson(new TestModel(Guid.NewGuid().ToString()));
            return model.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions);
        }, expectedFolder, AppConstants.ErrorOutputFilePrefix, AppConstants.ApiSourceExpectedErrorFolder);
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
