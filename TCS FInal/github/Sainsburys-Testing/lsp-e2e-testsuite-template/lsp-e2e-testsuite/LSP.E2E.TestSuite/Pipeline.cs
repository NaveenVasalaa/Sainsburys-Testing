namespace LSP.E2E.TestSuite;

public class Pipeline : ITestPipelineStartup
{
    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        if (Directory.Exists(AppConstants.BasePath))
            Directory.Delete(AppConstants.BasePath, true);

        Directory.CreateDirectory(AppConstants.TestLogFolder);
        Directory.CreateDirectory(AppConstants.TestReportFolder);

        FileHelper.AppendLine(AppConstants.LogFile, $"Executed Date Time: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        FileHelper.AppendLine(AppConstants.LogFile, new string('=', 120));

        FileHelper.CopyAllFiles(AppConstants.TestDataPath, AppConstants.TestDataFolder);
        MongoExtensions.SetConventions();

        ApiSourceTestsController.ComparerConfigure();
        FileSourceTestsController.ComparerConfigure();
        KafkaSourceTestsController.ComparerConfigure();

        return new ValueTask();
    }

    public ValueTask StopAsync()
    {
        ResultExtensions.UpdateFinalResult(AppConstants.LogFile);
        return new ValueTask();
    }
}
