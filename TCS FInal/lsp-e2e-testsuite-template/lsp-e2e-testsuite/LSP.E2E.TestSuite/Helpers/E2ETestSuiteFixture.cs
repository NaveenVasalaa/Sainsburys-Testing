using LogisticsPlatform.Testing.Extensions.Models;
using MongoDB.Bson;

namespace LSP.E2E.TestSuite.Helpers
{

    [CollectionDefinition("ServiceNameE2E Collection")]
    public class ServiceNameE2ECollection : ICollectionFixture<E2ETestSuiteFixture>
    {
        // This class contains no code and is used only for fixture initialization for the test collection.
    }


    public class E2ETestSuiteFixture : IDisposable
    {
        private readonly E2EMongoController _e2EMongoController;

        public DateTime _testexecutionStartTime { get; }
        public DateTime EndTime { get; private set; }

        // This runs before any [Theory] in the test class
        // Place your Before Theories logic here
        public E2ETestSuiteFixture()
        {
            var mongoE2EHelper = new MongoHelper<BsonDocument>(AppConstants.MongoConnectionString, AppConstants.MongoDbName, AppConstants.E2ECollectionName, AppConstants.LogFile);

            _e2EMongoController = new E2EMongoController(mongoE2EHelper);
            _testexecutionStartTime = DateTime.UtcNow;

            //TODO: Update ServiceName based on your E2E collection data, Ex: V1Service, V2Service etc.
            var message = _e2EMongoController.DownloadE2EMongoDataAsync("ServiceName");
            AppConstants.E2ESuiteRunConfig = message.Result;
       
        }

        public void Dispose()
        {
            var testExecutionstartTime = _testexecutionStartTime;
            DateTime totalTestsExecutionEndTime = DateTime.UtcNow;

            //TODO: Update and Add the ConfigData dictionary with the relevant configuration values that you want to include in the test report.
            var ConfigData = new Dictionary<string, bool>
            {
                { "ApiSourceComponentNameEnabled", AppConstants.E2ESuiteRunConfig.ApiSourceComponentNameEnabled },
                {"FileSourceComponentNameEnabled", AppConstants.E2ESuiteRunConfig.FileSourceComponentNameEnabled },
                 {"KafkaSourceComponentNameEnabled", AppConstants.E2ESuiteRunConfig.KafkaSourceComponentNameEnabled }

            };

            HtmlReportHelper.GenerateTestResultsHtmlReport(testExecutionstartTime, totalTestsExecutionEndTime, AppConstants.ServiceName, TestResultLogs.ScenarioResults, AppConstants.TestReportFolder, ConfigData);
            
            if (TestsComponentToggles.EmailReportEnabled)
            {
                EmailHelper.SendEmail(AppConstants.E2ESuiteRunConfig.TestReportMailRecipients, $"{AppConstants.Subject} on {DateTime.Now:dd/MM/yyyy}", AppConstants.TestReportFolder, TestResultLogs.ScenarioResults);
            }

            GC.SuppressFinalize(this);
        }
    }

    public static class TestsComponentToggles
    {
        //TODO: Update Add Components as per your requirement, and use these toggles in your test classes to conditionally run tests based on the configuration values fetched from MongoDB.
        public static bool RunApiSourceComponentTests = AppConstants.E2ESuiteRunConfig.ApiSourceComponentNameEnabled;
        public static bool RunFileSourceComponentTests = AppConstants.E2ESuiteRunConfig.FileSourceComponentNameEnabled;
        public static bool RunKafkaSourceComponentTests = AppConstants.E2ESuiteRunConfig.KafkaSourceComponentNameEnabled;
        public static bool EmailReportEnabled = AppConstants.E2ESuiteRunConfig.EmailReportEnabled;
    }
}
