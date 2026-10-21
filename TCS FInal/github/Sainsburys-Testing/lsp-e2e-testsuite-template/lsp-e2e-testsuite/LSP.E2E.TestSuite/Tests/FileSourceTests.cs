using LogisticsPlatform.Testing.Extensions.Models;
using LSP.E2E.TestSuite.Helpers;

namespace LogisticsPlatform.ACL.Publish.IntgTest.Tests;

[Collection("ServiceNameE2E Collection")]
public class FileSourceTests 
{
    private readonly Dictionary<string, string> _createdDirectories;
    private readonly DateTime _executionStartTime;
    private readonly FileSourceTestsController _fileSourceTestsController;
    private readonly E2ETestSuiteFixture _fixture;

    public FileSourceTests(E2ETestSuiteFixture fixture)
    {
        _fixture = fixture;
        if (!TestsComponentToggles.RunFileSourceComponentTests)
        {
            Assert.Skip("File Source tests are disabled");
        }
        var title = TestContextExtensions.GetScenarioName();
        _executionStartTime = DateTime.UtcNow;
        _createdDirectories = FileHelper.CreateAllDirectories(AppConstants.BasePath, AppConstants.FileSourceRelativePaths, title.ToString(), AppConstants.LogFile);
        var mongoHelper = new MongoHelper<ModelJson>(AppConstants.MongoConnectionString, AppConstants.MongoDbName, AppConstants.MasterCollectionName, AppConstants.LogFile);
        var kafkaHelper = new KafkaHelper(AppConstants.KafkaBroker, AppConstants.KafkaUsername, AppConstants.KafkaPassword, AppConstants.LogFile);
        var kafkaAvroHelper = new KafkaAvroHelper<ModelJson>(AppConstants.KafkaBroker, AppConstants.KafkaUsername, AppConstants.KafkaPassword, AppConstants.SchemaRegistryUrl, AppConstants.KafkaSchemaRegistryAuth, AppConstants.LogFile);
        //TODO: Remove one of the below initializations(azureHelper/mqHelper) based on the interface
        var azureHelper = new AzureHelper(AppConstants.BlobConnectionString, AppConstants.BlobContainer, AppConstants.LogFile);
        var mqHelper = new MQHelper(new Testing.Extensions.Models.MQOptions()
        {
            HostName = AppConstants.HostName,
            PortNumber = AppConstants.PortNumber,
            ChannelId = AppConstants.ChannelId,
            QueueManager = AppConstants.QueueManager,
            AccessQueue = AppConstants.AccessQueue
        }, AppConstants.LogFile);
        _fileSourceTestsController = new FileSourceTestsController(mongoHelper, kafkaHelper, kafkaAvroHelper, azureHelper, mqHelper);
    }

    [Theory]
    [ClassData(typeof(FileSourcePositiveTestScenarios))]
    public async Task FileSourcePositiveTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
    {
        FileHelper.AppendLine(AppConstants.LogFile, $"Test Case: {testCase}");
        FileHelper.AppendLine(AppConstants.LogFile, $"Scenario Description: {scenarioDescription}");
        FileHelper.AppendLine(AppConstants.LogFile, new string('=', 100));

        //TODO: Update the component, component name and other details as per actual scenario
        string component = "ACL/Adaptor";
        string componentName = "ComponentName";

        AppConstants.ScenarioResult = new ScenarioResult { ScenarioName = testCase, ScenarioType = scenarioType, ScenarioDescription = scenarioDescription, Component = component, ComponentName = componentName, AdaptorsCovered = adaptorsCovered, ErrorMessage = new StringBuilder() };

        await _fileSourceTestsController.DeleteAllAzureFilesAsync(AppConstants.BlobFilePath);
        await _fileSourceTestsController.DeleteAllDocumentsAsync();
        try
        {
            _fileSourceTestsController.GenerateInputFiles(testCase, AppConstants.FileSourceInputWorkSheetName, _createdDirectories[AppConstants.FileSourceInputFolder]);
            _fileSourceTestsController.GenerateExpectedInboundKafkaFiles(testCase, AppConstants.FileSourceExpectedInboundWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedInboundFolder]);
            _fileSourceTestsController.GenerateExpectedOutboundKafkaFiles(testCase, AppConstants.FileSourceExpectedOutboundWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedOutboundFolder]);

            //TODO: Remove one of the below method calling(PostMessagesToMQ/UploadAllFilesToAzureAsync) based on the interface
            _fileSourceTestsController.PostMessagesToMQ(_createdDirectories[AppConstants.FileSourceInputFolder]);
            await _fileSourceTestsController.UploadAllFilesToAzureAsync(AppConstants.BlobFilePath, _createdDirectories[AppConstants.FileSourceInputFolder]);

            await Task.Delay(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken); // Wait for the messages to be processed

            _fileSourceTestsController.ConsumeMessageFromKafka(AppConstants.InboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.FileSourceActualInboundFolder], AppConstants.OutputFilePrefix);
            _fileSourceTestsController.ConsumeMessageFromKafka(AppConstants.OutboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.FileSourceActualOutboundFolder], AppConstants.OutputFilePrefix);
            _fileSourceTestsController.ConsumeAvroMessageFromKafka(AppConstants.OutboundAvroTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.FileSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix);

            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                _fileSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.FileSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder]);
                await _fileSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }
            if (adaptorsCovered.Contains("Adaptor2Name"))
            {
                _fileSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.FileSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder]);
                await _fileSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }


            Record(FileSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedInboundFolder], _createdDirectories[AppConstants.FileSourceActualInboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedInboundWorkSheetName), "FileSourceInbound");
            Record(FileSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedOutboundFolder], _createdDirectories[AppConstants.FileSourceActualOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedOutboundWorkSheetName), "FileSourceOutbound");
            Record(FileSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedOutboundFolder], _createdDirectories[AppConstants.FileSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedOutboundWorkSheetName), "FileSourceOutboundAvro");

            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                var headerDefinition = SectionConfig.GetHeaderDefinition();
                var contentDefinition = SectionConfig.GetContentDefinitions();
                var trailerDefinition = SectionConfig.GetTrailerDefinition();

                Record(new FileComparisonHelper(_createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder], _createdDirectories[AppConstants.FileSourceActualAdaptorFolder   ], AppConstants.OutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage, "Adaptor1Name")
                .CompareFlatFiles(headerDefinition, contentDefinition, trailerDefinition), "Adaptor1Name");
            }

            if (adaptorsCovered.Contains("Adaptor2Name"))
            {
                var headerDefinition = SectionConfig.GetHeaderDefinition();
                var contentDefinition = SectionConfig.GetContentDefinitions();
                var trailerDefinition = SectionConfig.GetTrailerDefinition();

                Record(new FileComparisonHelper(_createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder], _createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage, "Adaptor2Name")
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
    [ClassData(typeof(FileSourceNegativeTestScenarios))]
    public async Task FileSourceNegativeTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
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
            _fileSourceTestsController.GenerateInputFiles(testCase, AppConstants.FileSourceInputWorkSheetName, _createdDirectories[AppConstants.FileSourceInputFolder]);
            await _fileSourceTestsController.PostMessageToApiAsync(AppConstants.ApiUrl, _createdDirectories[AppConstants.FileSourceInputFolder], $"{_createdDirectories[AppConstants.FileSourceActualErrorFolder]}/{AppConstants.ErrorOutputFilePrefix}");

            FileHelper.AppendLine(AppConstants.LogFile, new string('-', 100));

            Record(_fileSourceTestsController.VerifyNoOutput(_createdDirectories[AppConstants.FileSourceActualErrorFolder], AppConstants.ErrorOutputFilePrefix,AppConstants.LogFile ,AppConstants.ScenarioResult.ErrorMessage),"NegativeScenario");

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
