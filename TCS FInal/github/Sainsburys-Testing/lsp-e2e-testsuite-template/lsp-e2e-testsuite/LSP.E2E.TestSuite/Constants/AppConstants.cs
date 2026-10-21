using LogisticsPlatform.Testing.Extensions.Models;
#pragma warning disable CA2211 // Suppress warning for mutable static fields, as they are intentionally designed to be updated during test execution.

namespace LSP.E2E.TestSuite.Constants;

public static class AppConstants
{
    //TODO: Update and Add the common constants as per requirement
    #region Common Constants    
    public static readonly string TestDataFolderName = "Testdata";
    public static readonly string LogFolderName = "TestResults";
    public static readonly string CommonKeyVaultName = "lsp-kv-common-nonprd";
    public static readonly string BasePath = FileHelper.GetTestFilePath(LogFolderName, "");
    public static readonly string TestDataPath = FileHelper.GetTestFilePath(TestDataFolderName, "");
    public static readonly string TestDataFolder = Path.Combine(BasePath, $"{TestDataFolderName}/");
    public static readonly string TestLogFolder = Path.Combine(BasePath, "logs/");
    public static readonly string CopyFile = Path.Combine(BasePath, @"txtFile/");
    public static readonly string TestReportFolder = Path.Combine(BasePath, "HtmlReport/");

    public static readonly string LogFile = Path.Combine(TestLogFolder, $"TestLog_{DateTime.UtcNow:ddMMyyyyHHmmss}.txt");
    public static readonly string HTMLReportFile = Path.Combine(TestReportFolder, $"HtmlTestReport{DateTime.Now.Date.ToString("yyyy-MM-dd")}.html");
    #endregion

    //TODO: Update constants for other APISource scenarios, and add constants for other Source as per requirement
    #region ApiSource Constants
    public static readonly string ApiSource_TestDataFile = Path.Combine(TestDataFolder, "ApiSource_TestData.xlsx");
    public const string ApiSourceInputWorkSheetName = "ApiSource_InputFile";
    public const string ApiSourceErrorInputWorkSheetName = "ErrorInputFile";
    public const string ApiSourceExpectedInboundWorkSheetName = "ApiSource_Inbound";
    public const string ApiSourceExpectedOutboundWorkSheetName = "ApiSource_Outbound";
    public const string ApiSourceExpectedErrorWorkSheetName = "Error_Output";
    public const string ApiSourceExpectedAdaptorWorkSheetName = "Adaptor_Output";

    public const string ApiSourceInputFolder = "ApiSourceInputFolder";
    public const string ApiSourceExpectedInboundFolder = "ApiSourceExpectedInboundFolder";
    public const string ApiSourceExpectedOutboundFolder = "ApiSourceExpectedOutboundFolder";
    public const string ApiSourceExpectedAvroOutboundFolder = "ApiSourceExpectedAvroOutboundFolder";
    public const string ApiSourceExpectedAdaptorFolder = "ApiSourceExpectedAdaptorFolder";
    public const string ApiSourceExpectedErrorFolder = "ApiSourceExpectedErrorFolder";
    public const string ApiSourceActualInboundFolder = "ApiSourceActualInboundFolder";
    public const string ApiSourceActualOutboundFolder = "ApiSourceActualOutboundFolder";
    public const string ApiSourceActualAvroOutboundFolder = "ApiSourceActualAvroOutboundFolder";
    public const string ApiSourceActualAdaptorFolder = "ApiSourceActualAdaptorFolder";
    public const string ApiSourceActualErrorFolder = "ApiSourceActualErrorFolder";

    public static readonly Dictionary<string, string> ApiSourceRelativePaths = new()
    {
        { ApiSourceInputFolder, @"apiSource_input/" },
        { ApiSourceExpectedInboundFolder, @"apiSource_expected/inbound/" },
        { ApiSourceExpectedOutboundFolder, @"apiSource_expected/outbound/" },
        { ApiSourceExpectedAvroOutboundFolder, @"apiSource_expected/outboundavro/" },
        { ApiSourceExpectedAdaptorFolder, @"apiSource_expected/adaptor/" },
        { ApiSourceExpectedErrorFolder, @"apiSource_expected/Error/" },

        { ApiSourceActualInboundFolder, @"apiSource_actual/inbound/" },
        { ApiSourceActualOutboundFolder, @"apiSource_actual/outbound/"},
        { ApiSourceActualAvroOutboundFolder, @"apiSource_actual/outboundavro/" },
        { ApiSourceActualAdaptorFolder, @"apiSource_actual/adaptor/" },
        { ApiSourceActualErrorFolder, @"apiSource_actual/Error/" }
    };

    public static readonly string KeyVaultName = "lsp-kv-<replace>-nonprd";
    public static readonly string MongoConnectionString = AzureHelper.GetSecret(KeyVaultName, Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "MONGO-OIDC-CONNECTION-INTGTST");
    public static readonly string MongoDbName = "<replace>-tst";
    public static readonly string MasterCollectionName = "<replace>";
    public static readonly string ApiUrl = "<replace>";
    public static readonly string SchemaRegistryUrl = "https://psrc-lg26v.westeurope.azure.confluent.cloud";
    public static readonly string TenantId = AzureHelper.GetSecret(CommonKeyVaultName, "AZURE-TENANT-ID");
    public static readonly string ClientId = AzureHelper.GetSecret(CommonKeyVaultName, "AZURE-TESTING-CLIENT-ID-NONPROD");
    public static readonly string ClientSecret = AzureHelper.GetSecret(CommonKeyVaultName, "AZURE-TESTING-CLIENT-SECRET-NONPROD");
    public static readonly string ResourceId = AzureHelper.GetSecret(CommonKeyVaultName, "IGD-RESOURCE-ID");
    public static readonly string KafkaBroker = "pkc-9wy77.northeurope.azure.confluent.cloud:9092";
    public static readonly string KafkaUsername = AzureHelper.GetSecret(KeyVaultName, "KAFKA-USERNAME-NONPROD");
    public static readonly string KafkaPassword = AzureHelper.GetSecret(KeyVaultName, "KAFKA-PASSWORD-NONPROD");
    public static readonly string KafkaSchemaRegistryAuth = AzureHelper.GetSecret(KeyVaultName, "AVRO-SCHEMA-REGISTRY-AUTH-NONPROD");
    public static readonly string InboundTopic = "js-logistics-<replace>-inbound-v1-tst";
    public static readonly string OutboundTopic = "js-logistics-<replace>-v1-tst";
    public static readonly string OutboundAvroTopic = "js-logistics-<replace>-v2-tst";
    public static readonly string KafkaConsumerGroup = "sainsburys.application.lsp.<replace>.intgtst";
    public static readonly string InputFilePrefix = "Input_";
    public static readonly string OutputFilePrefix = "Output_";
    public static readonly string ErrorOutputFilePrefix = "ErrorOutput_";
    #endregion

    #region FileSourceConstants
    public static readonly string FileSource_TestDataFile = Path.Combine(TestDataFolder, "FileSource_TestData.xlsx");
    public const string FileSourceInputWorkSheetName = "FileSource_InputFile";
    public const string FileSourceErrorInputWorkSheetName = "ErrorInputFile";
    public const string FileSourceExpectedInboundWorkSheetName = "FileSource_Inbound";
    public const string FileSourceExpectedOutboundWorkSheetName = "FileSource_Outbound";
    public const string FileSourceExpectedErrorWorkSheetName = "Error_Output";
    public const string FileSourceExpectedAdaptorWorkSheetName = "Adaptor_Output";

    public const string FileSourceInputFolder = "FileSourceInputFolder";
    public const string FileSourceExpectedInboundFolder = "FileSourceExpectedInboundFolder";
    public const string FileSourceExpectedOutboundFolder = "FileSourceExpectedOutboundFolder";
    public const string FileSourceExpectedAvroOutboundFolder = "FileSourceExpectedAvroOutboundFolder";
    public const string FileSourceExpectedAdaptorFolder = "FileSourceExpectedAdaptorFolder";
    public const string FileSourceExpectedErrorFolder = "FileSourceExpectedErrorFolder";
    public const string FileSourceActualInboundFolder = "FileSourceActualInboundFolder";
    public const string FileSourceActualOutboundFolder = "FileSourceActualOutboundFolder";
    public const string FileSourceActualAvroOutboundFolder = "FileSourceActualAvroOutboundFolder";
    public const string FileSourceActualAdaptorFolder = "FileSourceActualAdaptorFolder";
    public const string FileSourceActualErrorFolder = "FileSourceActualErrorFolder";

    public static readonly Dictionary<string, string> FileSourceRelativePaths = new()
    {
        { FileSourceInputFolder, @"FileSource_input/" },
        { FileSourceExpectedInboundFolder, @"FileSource_expected/inbound/" },
        { FileSourceExpectedOutboundFolder, @"FileSource_expected/outbound/" },
        { FileSourceExpectedAvroOutboundFolder, @"FileSource_expected/outboundavro/" },
        { FileSourceExpectedAdaptorFolder, @"FileSource_expected/adaptor/" },
        { FileSourceExpectedErrorFolder, @"FileSource_expected/Error/" },

        { FileSourceActualInboundFolder, @"FileSource_actual/inbound/" },
        { FileSourceActualOutboundFolder, @"FileSource_actual/outbound/"},
        { FileSourceActualAvroOutboundFolder, @"FileSource_actual/outboundavro/" },
        { FileSourceActualAdaptorFolder, @"FileSource_actual/adaptor/" },
        { FileSourceActualErrorFolder, @"FileSource_actual/Error/" }
    };


    // TODO: Replace the placeholder <replace> with relevant values    
    public static readonly string BlobConnectionString = AzureHelper.GetSecret(KeyVaultName, "AZURE-STORAGE-ACCOUNT-NONPROD");
    public static readonly string BlobContainer = "acl-tst";
    public static readonly string BlobFilePath = "<replace>";
    public static readonly string HostName = "<replace>";
    public static readonly string PortNumber = "<replace>";
    public static readonly string ChannelId = "<replace>";
    public static readonly string QueueManager = "<replace>";
    public static readonly string AccessQueue = "<replace>";

    #endregion

    #region KafkaSourceConstants
    public static readonly string KafkaSource_TestDataFile = Path.Combine(TestDataFolder, "KafkaSource_TestData.xlsx");
    public const string KafkaSourceInputWorkSheetName = "KafkaSource_InputFile";
    public const string KafkaSourceErrorInputWorkSheetName = "ErrorInputFile";
    public const string KafkaSourceExpectedInboundWorkSheetName = "KafkaSource_Inbound";
    public const string KafkaSourceExpectedOutboundWorkSheetName = "KafkaSource_Outbound";
    public const string KafkaSourceExpectedErrorWorkSheetName = "Error_Output";
    public const string KafkaSourceExpectedAdaptorWorkSheetName = "Adaptor_Output";

    public const string KafkaSourceInputFolder = "KafkaSourceInputFolder";
    public const string KafkaSourceExpectedInboundFolder = "KafkaSourceExpectedInboundFolder";
    public const string KafkaSourceExpectedOutboundFolder = "KafkaSourceExpectedOutboundFolder";
    public const string KafkaSourceExpectedAvroOutboundFolder = "KafkaSourceExpectedAvroOutboundFolder";
    public const string KafkaSourceExpectedAdaptorFolder = "KafkaSourceExpectedAdaptorFolder";
    public const string KafkaSourceExpectedErrorFolder = "KafkaSourceExpectedErrorFolder";
    public const string KafkaSourceActualInboundFolder = "KafkaSourceActualInboundFolder";
    public const string KafkaSourceActualOutboundFolder = "KafkaSourceActualOutboundFolder";
    public const string KafkaSourceActualAvroOutboundFolder = "KafkaSourceActualAvroOutboundFolder";
    public const string KafkaSourceActualAdaptorFolder = "KafkaSourceActualAdaptorFolder";
    public const string KafkaSourceActualErrorFolder = "KafkaSourceActualErrorFolder";

    public static readonly Dictionary<string, string> KafkaSourceRelativePaths = new()
    {
        { KafkaSourceInputFolder, @"KafkaSource_input/" },
        { KafkaSourceExpectedInboundFolder, @"KafkaSource_expected/inbound/" },
        { KafkaSourceExpectedOutboundFolder, @"KafkaSource_expected/outbound/" },
        { KafkaSourceExpectedAvroOutboundFolder, @"KafkaSource_expected/outboundavro/" },
        { KafkaSourceExpectedAdaptorFolder, @"KafkaSource_expected/adaptor/" },
        { KafkaSourceExpectedErrorFolder, @"KafkaSource_expected/Error/" },

        { KafkaSourceActualInboundFolder, @"KafkaSource_actual/inbound/" },
        { KafkaSourceActualOutboundFolder, @"KafkaSource_actual/outbound/"},
        { KafkaSourceActualAvroOutboundFolder, @"KafkaSource_actual/outboundavro/" },
        { KafkaSourceActualAdaptorFolder, @"KafkaSource_actual/adaptor/" },
        { KafkaSourceActualErrorFolder, @"KafkaSource_actual/Error/" }
    };
    #endregion


    //TODO: Update and add constants for HTML report as per requirement
    #region Html Report Constants
    public static readonly string ServiceName = "LSP-ServiceName";
    public static E2ESuiteRunConfig E2ESuiteRunConfig { get; set; }
    public static StringBuilder Errorsb = new();
    public static ScenarioResult ScenarioResult = new();
    #endregion

    //TODO: Update and add constants for Email as per requirement
    #region EmailConstans
    public static readonly string Subject = "E2E Test Results";
    #endregion

    //TODO: Update E2ESuiteRunConfig Collection name
    #region E2ESuiteRunConfig Constants
    public static readonly string E2ECollectionName = "E2ESuiteRunConfig";
   
    #endregion

}
