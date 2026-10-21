using LogisticsPlatform.Testing.Extensions.Models;
using LSP.E2E.TestSuite.Helpers;

namespace LogisticsPlatform.ACL.Publish.IntgTest.Tests;

[Collection("ServiceNameE2E Collection")]
public class KafkaSourceTests 
{
    private readonly Dictionary<string, string> _createdDirectories;
    private readonly DateTime _executionStartTime;
    private readonly KafkaSourceTestsController _KafkaSourceTestsController;
    private readonly E2ETestSuiteFixture _fixture;
    public KafkaSourceTests(E2ETestSuiteFixture fixture)
    {
        _fixture = fixture;
        if (!TestsComponentToggles.RunKafkaSourceComponentTests)
        {
            Assert.Skip("Kafka Source tests are disabled");
        }
        var title = TestContextExtensions.GetScenarioName();
        _executionStartTime = DateTime.UtcNow;
        _createdDirectories = FileHelper.CreateAllDirectories(AppConstants.BasePath, AppConstants.KafkaSourceRelativePaths, title.ToString(), AppConstants.LogFile);
        var mongoHelper = new MongoHelper<ModelJson>(AppConstants.MongoConnectionString, AppConstants.MongoDbName, AppConstants.MasterCollectionName, AppConstants.LogFile);
        var kafkaHelper = new KafkaHelper(AppConstants.KafkaBroker, AppConstants.KafkaUsername, AppConstants.KafkaPassword, AppConstants.LogFile);
        _KafkaSourceTestsController = new KafkaSourceTestsController(mongoHelper, kafkaHelper);
    }

    [Theory]
    [ClassData(typeof(KafkaSourcePositiveTestScenarios))]
    public async Task KafkaSourcepositiveTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
    {
        FileHelper.AppendLine(AppConstants.LogFile, $"Test Case: {testCase}");
        FileHelper.AppendLine(AppConstants.LogFile, $"Scenario Description: {scenarioDescription}");
        FileHelper.AppendLine(AppConstants.LogFile, new string('=', 100));

        string component = "ACL/Adaptor";
        string componentName = "ComponentName";

        AppConstants.ScenarioResult = new ScenarioResult { ScenarioName = testCase, ScenarioType = scenarioType, ScenarioDescription = scenarioDescription, Component = component, ComponentName = componentName, AdaptorsCovered = adaptorsCovered, ErrorMessage = new StringBuilder()};

        try
        {


            _KafkaSourceTestsController.GenerateInputFiles(testCase, AppConstants.KafkaSourceInputWorkSheetName, _createdDirectories[AppConstants.KafkaSourceInputFolder]);
            _KafkaSourceTestsController.GenerateExpectedInboundKafkaFiles(testCase, AppConstants.KafkaSourceExpectedInboundWorkSheetName, _createdDirectories[AppConstants.KafkaSourceExpectedInboundFolder]);
            _KafkaSourceTestsController.GenerateExpectedOutboundKafkaFiles(testCase, AppConstants.KafkaSourceExpectedOutboundWorkSheetName, _createdDirectories[AppConstants.KafkaSourceExpectedOutboundFolder]);

            await _KafkaSourceTestsController.SendMessageToKafkaAsync(AppConstants.KafkaSourceInputFolder, DateTime.Now.Ticks.ToString(), _createdDirectories[AppConstants.KafkaSourceInputFolder]);

            _KafkaSourceTestsController.ConsumeMessageFromKafka(AppConstants.InboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.KafkaSourceActualInboundFolder], AppConstants.OutputFilePrefix);
            _KafkaSourceTestsController.ConsumeMessageFromKafka(AppConstants.OutboundTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.KafkaSourceActualOutboundFolder], AppConstants.OutputFilePrefix);
            _KafkaSourceTestsController.ConsumeAvroMessageFromKafka(AppConstants.OutboundAvroTopic, AppConstants.KafkaConsumerGroup, 10, _executionStartTime, _createdDirectories[AppConstants.KafkaSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix);

            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                _KafkaSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.FileSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder]);
                await _KafkaSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }
            if (adaptorsCovered.Contains("Adaptor2Name"))
            {
                _KafkaSourceTestsController.GenerateExpectedAdaptorOutputFiles(testCase, AppConstants.FileSourceExpectedAdaptorWorkSheetName, _createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder]);
                await _KafkaSourceTestsController.DownloadMongoEntriesAsync(_createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix);
            }


            Record(KafkaSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedInboundFolder], _createdDirectories[AppConstants.FileSourceActualInboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedInboundWorkSheetName), "FileSourceInbound");
            Record(KafkaSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedOutboundFolder], _createdDirectories[AppConstants.FileSourceActualOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedOutboundWorkSheetName), "FileSourceOutbound");
            Record(KafkaSourceTestsController.CompareOutputFiles(_createdDirectories[AppConstants.FileSourceExpectedOutboundFolder], _createdDirectories[AppConstants.FileSourceActualAvroOutboundFolder], AppConstants.OutputFilePrefix, AppConstants.FileSourceExpectedOutboundWorkSheetName), "FileSourceOutboundAvro");

            if (adaptorsCovered.Contains("Adaptor1Name"))
            {
                var headerDefinition = SectionConfig.GetHeaderDefinition();
                var contentDefinition = SectionConfig.GetContentDefinitions();
                var trailerDefinition = SectionConfig.GetTrailerDefinition();

                Record(new FileComparisonHelper(_createdDirectories[AppConstants.FileSourceExpectedAdaptorFolder], _createdDirectories[AppConstants.FileSourceActualAdaptorFolder], AppConstants.OutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage, "Adaptor1Name")
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
    [ClassData(typeof(KafkaSourceNegativeTestScenarios))]
    public async Task KafkaSourceNegativeTest(string testCase, string scenarioDescription, string adaptorsCovered, string scenarioType)
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
            _KafkaSourceTestsController.GenerateInputFiles(testCase, AppConstants.KafkaSourceInputWorkSheetName, _createdDirectories[AppConstants.KafkaSourceInputFolder]);

            await _KafkaSourceTestsController.SendMessageToKafkaAsync(AppConstants.KafkaSourceInputFolder, DateTime.Now.Ticks.ToString(), _createdDirectories[AppConstants.KafkaSourceInputFolder]);

            FileHelper.AppendLine(AppConstants.LogFile, new string('-', 100));

            Record(_KafkaSourceTestsController.VerifyNoOutput(_createdDirectories[AppConstants.KafkaSourceActualErrorFolder], AppConstants.ErrorOutputFilePrefix, AppConstants.LogFile, AppConstants.ScenarioResult.ErrorMessage), "NegativeScenario");

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
