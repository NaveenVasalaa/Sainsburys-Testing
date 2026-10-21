using LogisticsPlatform.Testing.Extensions.Models;
using LSP.E2E.TestSuite.Helpers;

namespace LogisticsPlatform.ACL.Publish.IntgTest.Tests;

[Collection("ServiceNameE2E Collection")]
public class ApiSourceTests
{
    private readonly Dictionary<string, string> _createdDirectories;
    private readonly DateTime _executionStartTime;
    private readonly ApiSourceTestsController _apiSourceTestsController;
    private readonly E2ETestSuiteFixture _fixture;

    public ApiSourceTests(E2ETestSuiteFixture fixture)
    {
        _fixture = fixture;
        if (!TestsComponentToggles.RunApiSourceComponentTests)
        {
            Assert.Skip("Api Source tests are disabled");
        }

        var title = TestContextExtensions.GetScenarioName();
        _executionStartTime = DateTime.UtcNow;
        _createdDirectories = FileHelper.CreateAllDirectories(AppConstants.BasePath, AppConstants.ApiSourceRelativePaths, title, AppConstants.LogFile);
        var mongoHelper = new MongoHelper<ModelJson>(AppConstants.MongoConnectionString, AppConstants.MongoDbName, AppConstants.MasterCollectionName, AppConstants.LogFile);
        var kafkaHelper = new KafkaHelper(AppConstants.KafkaBroker, AppConstants.KafkaUsername, AppConstants.KafkaPassword, AppConstants.LogFile);
        var kafkaAvroHelper = new KafkaAvroHelper<ModelJson>(AppConstants.KafkaBroker, AppConstants.KafkaUsername, AppConstants.KafkaPassword, AppConstants.SchemaRegistryUrl, AppConstants.KafkaSchemaRegistryAuth, AppConstants.LogFile);
        var apiHelper = new ApiHelper(new ApiOptions
        {
            ApiClientId = AppConstants.ClientId,
            ApiClientSecret = AppConstants.ClientSecret,
            ApiTenantId = AppConstants.TenantId,
            ApiResourceId = AppConstants.ResourceId,
            LogFilePath = AppConstants.LogFile,
        });
        _apiSourceTestsController = new ApiSourceTestsController(mongoHelper, kafkaHelper, kafkaAvroHelper, apiHelper);
    }

    [Theory]
    [ClassData(typeof(ApiSourcePositiveTestScenarios))]
    public async Task ApiSourcePositiveTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
    {
        FileHelper.AppendLine(AppConstants.LogFile, $"Test Case: {testCase}");
        FileHelper.AppendLine(AppConstants.LogFile, $"Scenario Description: {scenarioDescription}");
        FileHelper.AppendLine(AppConstants.LogFile, new string('=', 100));

        //TODO: Update the component, component name and other details as per actual scenario
        string component = "ACL/Adaptor";
        string componentName = "ComponentName";

       

        AppConstants.ScenarioResult = new ScenarioResult { ScenarioName = testCase, ScenarioType = scenarioType, ScenarioDescription = scenarioDescription,Component= component, ComponentName = componentName, AdaptorsCovered = adaptorsCovered, ErrorMessage = new StringBuilder() };

        try
        {
            await _apiSourceTestsController.DeleteAllDocumentsAsync();

            _apiSourceTestsController.GenerateInputFiles(testCase, AppConstants.ApiSourceInputWorkSheetName, _createdDirectories[AppConstants.ApiSourceInputFolder]);
            _apiSourceTestsController.GenerateExpectedInboundKafkaFiles(testCase, AppConstants.ApiSourceExpectedInboundWorkSheetName, _createdDirectories[AppConstants.ApiSourceExpectedInboundFolder]);
            _apiSourceTestsController.GenerateExpectedOutboundKafkaFiles(testCase, AppConstants.ApiSourceExpectedOutboundWorkSheetName, _createdDirectories[AppConstants.ApiSourceExpectedOutboundFolder]);

            await _apiSourceTestsController.PostMessageToApiAsync(AppConstants.ApiUrl, _createdDirectories[AppConstants.ApiSourceInputFolder]);

            _apiSourceTestsController.ConsumeMessageFromKafka(AppConstants.InboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.ApiSourceActualInboundFolder], AppConstants.OutputFilePrefix);
            _apiSourceTestsController.ConsumeMessageFromKafka(AppConstants.OutboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.ApiSourceActualOutboundFolder], AppConstants.OutputFilePrefix);
            _apiSourceTestsController.ConsumeAvroMessageFromKafka(AppConstants.OutboundAvroTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.ApiSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix);

            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                _apiSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.ApiSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.ApiSourceExpectedAdaptorFolder]);
                await _apiSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.ApiSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }
            if (adaptorsCovered.Contains("Adaptor2Name"))
            {
                _apiSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.ApiSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.ApiSourceExpectedAdaptorFolder]);
                await _apiSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.ApiSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }

            Record(ApiSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.ApiSourceExpectedInboundFolder], _createdDirectories[AppConstants.ApiSourceActualInboundFolder], AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedInboundWorkSheetName), "ApiSourceInbound");
            Record(ApiSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.ApiSourceExpectedOutboundFolder], _createdDirectories[AppConstants.ApiSourceActualOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedOutboundWorkSheetName), "ApiSourceOutbound");
            Record(ApiSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.ApiSourceExpectedOutboundFolder], _createdDirectories[AppConstants.ApiSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.ApiSourceExpectedOutboundWorkSheetName), "ApiSourceOutboundAvro");
            
            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                var headerDefinition = SectionConfig.GetHeaderDefinition();
                var contentDefinition = SectionConfig.GetContentDefinitions();
                var trailerDefinition = SectionConfig.GetTrailerDefinition();
             
                Record(new FileComparisonHelper(_createdDirectories[AppConstants.ApiSourceExpectedAdaptorFolder], _createdDirectories[AppConstants.ApiSourceActualAdaptorFolder], AppConstants.OutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage, "Adaptor1Name")
                .CompareFlatFiles(headerDefinition, contentDefinition, trailerDefinition), "Adaptor1Name");
            }

            if (adaptorsCovered.Contains("Adaptor2Name"))
            {
                var headerDefinition = SectionConfig.GetHeaderDefinition();
                var contentDefinition = SectionConfig.GetContentDefinitions();
                var trailerDefinition = SectionConfig.GetTrailerDefinition();

                Record(new FileComparisonHelper(_createdDirectories[AppConstants.ApiSourceExpectedAdaptorFolder], _createdDirectories[AppConstants.ApiSourceActualAdaptorFolder], AppConstants.OutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage, "Adaptor2Name")
                .CompareFlatFiles(headerDefinition, contentDefinition, trailerDefinition), "Adaptor2Name");
            }

            void Record(bool success, string componentName)
            {
                (success ? AppConstants.ScenarioResult.PassedComponents : AppConstants.ScenarioResult.FailedComponents).Add(componentName);
            }
            AppConstants.ScenarioResult.Passed = AppConstants.ScenarioResult.FailedComponents.Count == 0;
            Assert.True(AppConstants.ScenarioResult.Passed);

        }
        catch (Exception ex)
        {
            AppConstants.ScenarioResult.Passed = false;
            AppConstants.ScenarioResult.ExceptionMessage = ex.ToString();
            throw;
        }
        finally
        {
            
            var status = AppConstants.ScenarioResult.Passed ? "Passed" : "Failed";
            ResultExtensions.StoreResult(status, AppConstants.LogFile, AppConstants.ScenarioResult.ExceptionMessage);
            TestResultLogs.ScenarioResults.Add(AppConstants.ScenarioResult);
        }
    }

    [Theory]
    [ClassData(typeof(ApiSourceNegativeTestScenarios))]
    public async Task ApiSourceNegativeTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
    {
        FileHelper.AppendLine(AppConstants.LogFile, $"Test Case: {testCase}");
        FileHelper.AppendLine(AppConstants.LogFile, $"Scenario Description: {scenarioDescription}");
        FileHelper.AppendLine(AppConstants.LogFile, new string('=', 100));

        //TODO: Update the component, component name and other details as per actual scenario
        string component = "ACL/Adaptor";
        string componentName = "ComponentName";

        AppConstants.ScenarioResult = new ScenarioResult { ScenarioName = testCase, ScenarioType = scenarioType, ScenarioDescription = scenarioDescription, Component = component, ComponentName = componentName, AdaptorsCovered = adaptorsCovered, ErrorMessage = new StringBuilder() };

        try
        {
            _apiSourceTestsController.GenerateInputFiles(testCase, AppConstants.ApiSourceInputWorkSheetName, _createdDirectories[AppConstants.ApiSourceInputFolder]);
            _apiSourceTestsController.GenerateExpectedOutputErrorFiles(testCase, AppConstants.ApiSourceExpectedErrorWorkSheetName, _createdDirectories[AppConstants.ApiSourceExpectedErrorFolder]);

            await _apiSourceTestsController.PostMessageToApiAsync(AppConstants.ApiUrl, _createdDirectories[AppConstants.ApiSourceInputFolder], $"{_createdDirectories[AppConstants.ApiSourceActualErrorFolder]}/{AppConstants.ErrorOutputFilePrefix}");

            FileHelper.AppendLine(AppConstants.LogFile, new string('-', 100));

            Record(ApiSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.ApiSourceExpectedErrorFolder], _createdDirectories[AppConstants.ApiSourceActualErrorFolder], AppConstants.ErrorOutputFilePrefix, AppConstants.ApiSourceExpectedErrorWorkSheetName),"ComponentName");

            void Record(bool success, string componentName)
            {
                (success ? AppConstants.ScenarioResult.PassedComponents : AppConstants.ScenarioResult.FailedComponents).Add(componentName);
            }
            AppConstants.ScenarioResult.Passed = AppConstants.ScenarioResult.FailedComponents.Count == 0;
            Assert.True(AppConstants.ScenarioResult.Passed);

        }
        catch (Exception ex)
        {
            AppConstants.ScenarioResult.Passed = false;
            AppConstants.ScenarioResult.ExceptionMessage = ex.ToString();
            throw;
        }
        finally
        { 
            var status = AppConstants.ScenarioResult.Passed ? "Passed" : "Failed";
            ResultExtensions.StoreResult(status, AppConstants.LogFile, AppConstants.ScenarioResult.ExceptionMessage);
            TestResultLogs.ScenarioResults.Add(AppConstants.ScenarioResult);
        }

    }

   
}

