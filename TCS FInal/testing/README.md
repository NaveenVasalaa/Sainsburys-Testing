# Testing extensions

Package name: `LogisticsPlatform.Testing.Extension`

## Introduction

This extension covers the common implementation required for the integration tests.

## Installation

You can install the package via NuGet:

```sh
dotnet add package LogisticsPlatform.Testing.Extension
```

## AzureHelper

This class provides all the necessary methods to interact with Azure Storage Blobs.

### Initialization

```c#
AzureHelper azureHelper = new AzureHelper(blobConnectionString, blobContainer, logFilePath);
```

- `blobConnectionString`: The connection string to the Azure Blob Storage account.
- `blobContainer`: The name of the blob container.
- `logFilePath`: The path to the log file where operations will be logged.

### CheckIfFileExistsAsync

```c#
azureHelper.CheckIfFileExistsAsync(fileNameWithPath);
```

- `fileNameWithPath`: Name of the file you want to check along with blob path. Example: `ManifestFromLPToDHL/AdaptorFile/Manifest_DHL_D000753725025.xlsx`


```c#
azureHelper.CheckIfFileExistsAsync(blobFilePath, filePrefix, waitTimeInSec);
```

- `blobFilePath`: Path from where you want to check the files if exists from blob storage. Example: `ManifestFromLPToDHL/AdaptorFile/`
- `filePrefix`: A list of file prefixes to filter the files to be checked. Example: `new List<string> { "SKU_COO" }`
- `waitTimeInSeconds`: The maximum elapsed time to check messages. Example: `60`

### DownloadFilesFromAzureBlobAsync

```c#
azureHelper.DownloadFilesFromAzureBlobAsync(blobFilePath, actualPath, filePrefix, waitTimeInSeconds, encryptionKeyVaultName, encryptionKeyName);
```

- `blobFilePath`: Path from where you want to download the files from blob storage. Example: `ManifestFromLPToDHL/AdaptorFile/`
- `actualPath`: Path where you want to store the downloaded files. Example: `TestResults/Actual/TC_001`
- `filePrefix`: A list of file prefixes to filter the files to be downloaded. Example: `new List<string> { "SKU_COO" }`
- `waitTimeInSeconds`: The maximum elapsed time to check messages. Example: `60`
- `encryptionKeyVaultName`: The name of the Azure Key Vault where the encryption key is stored. This parameter is optional and can be set to null if not required.
- `encryptionKeyName`: The name of the encryption key in the Azure Key Vault. This parameter is optional and can be set to null if not required.

### PutFileInAzureAsync

```c#
azureHelper.PutFileInAzureAsync(blobFilePath, inputPath);
```

- `blobFilePath`: Path where you want to upload the files in blob storage. Example: `ManifestFromLPToDHL/AdaptorFile/`
- `inputPath`: Path to the folder with files which you want to upload. Example: `TestResults/Input/TC_001/`

### PutStreamInAzureAsync
```c#
azureHelper.PutStreamInAzureAsync(blobFilePath, inputPath);
```

- `blobFilePath`: Path where you want to upload the file in blob storage along with the file name. Example: `ManifestFromLPToDHL/AdaptorFile/`
- `inputPath`: Path to the file which you want to upload. Example: `TestResults/Input/TC_001/`

### DeleteFilesinAzureBlobAsync

```c#
azureHelper.DeleteFilesinAzureBlobAsync(blobFilePath, new List<string> { });
```

- `blobFilePath`: Path from where you want to delete the files from blob storage. Example: `ManifestFromLPToDHL/AdaptorFile`
- `filePrefix`: A list of file prefixes to filter the files to be deleted. Example: `new List<string> { "SKU_COO" }`

### DeleteAllFilesinAzureBlobAsync

```c#
azureHelper.DeleteAllFilesinAzureBlobAsync(blobFilePath);
```

- `blobFilePath`: Path from where you want to delete the files from blob storage. Example: `ManifestFromLPToDHL/AdaptorFile`


## ApiHelper

This class provides the necessary method to post messages to an API.

### Initialization

```csharp
ApiOptions apiOptions = new()
{
		// Pass the client id, client secret, tenant id, and resource id
};

ApiHelper apiHelper = new(apiOptions);
```

- `clientId`: The client ID for the API.
- `clientSecret`: The client secret for the API.
- `tenantId`: The tenant ID for the API.
- `resourceId`: The resource ID for the API.
- `logFilePath`: The path to the log file where operations will be logged.

### PostMessage

```c#
await apiHelper.PostMessageAsync(apiUrl, inputPath, actualPathWithPrefix, authRequired, headers);
```

- `apiUrl`: An API URL you want to post the messages to.
- `inputPath`: Path to the files you want to post. Example: `TestResults/Input/TC_001/`
- `actualPathWithPrefix`: An optional parameter, Path where you want to store the response from the API. Example: `TestResults/Actual/TC_001/ActualFile_`
- `authRequired`: A boolean value indicating whether authentication is required. Example: `true` or `false`
- `headers`: A dictionary of custom headers to include in the request.Each key-value pair represents a header name and its corresponding value.

```c#
await apiHelper.PostMessageAsync(apiUrl, message, authRequired, headers);
```

- `apiUrl`: An API URL you want to post the messages to.
- `message`: Message you want to post to the API in string format.
- `authRequired`: A boolean value indicating whether authentication is required. Example: `true` or `false`
- `headers`: A dictionary of custom headers to include in the request. Each key-value pair represents a header name and its corresponding value.

### GetMessage

```c#
await apiHelper.GetMessageAsync(apiUrl, actualPath, outputFilePrefix, authRequired, headers);
```

- `apiUrl`: An API URL you want to get the messages from
- `actualPath`: Path where you want to store the response from the API. Example: `TestResults/Actual/TC_001`
- `outputFilePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_`
- `authRequired`: A boolean value indicating whether authentication is required. Example: `true` or `false`
- `headers`: A dictionary of custom headers to include in the request. Each key-value pair represents a header name and its corresponding value.

```c#
await apiHelper.GetMessageAsync(apiUrl, authRequired, headers);
```

- `apiUrl`: An API URL you want to get the messages from
- `authRequired`: A boolean value indicating whether authentication is required. Example: `true` or `false`
- `headers`: A dictionary of custom headers to include in the request. Each key-value pair represents a header name and its corresponding value.

## KafkaHelper

This class provides all the necessary methods to post messages to a Kafka topic and to consume messages from a Kafka topic.

### Initialization

```c#
KafkaHelper kafkaHelper = new KafkaHelper(kafkaBroker, kafkaUsername, kafkaPassword, logFilePath);
```

- `kafkaBroker`: The Kafka broker address.
- `kafkaUsername`: The username for Kafka authentication.
- `kafkaPassword`: The password for Kafka authentication.
- `logFilePath`: The file path for logging.

```c#
KafkaHelper kafkaHelper = new KafkaHelper(kafkaBroker, saslOauthbearerConfig, logFilePath);
```

- `kafkaBroker`: The Kafka broker address.
- `saslOauthbearerConfig`: The Kafka OIDC configuration for Kafka authentication.
- `logFilePath`: The file path for logging.

### ConsumeMessageFromKafka

```c#
kafkaHelper.ConsumeMessageFromKafka(topicName, consumerGroup, waitTimeInSeconds, offSetResetTime, actualPath, outputFilePrefix);
```

- `topicName`: The topic from where you want to consume the message. Example: `js-logistics-inbound-v1-tst`
- `consumerGroup`: The consumer group from which you want to consume the messages. Example: `sainsburys.application.lsp.<replace>.intgtst`
- `safetyTimeoutSeconds`: A ceiling to avoid hanging indefinitely if a broker error or unexpected gap occurs. Example: `60`
- `offSetResetTime`: To return the available offsets after the time mentioned. Example: `DateTime.UtcNow`
- `actualPath`: Path where you want to store the consumed files. Example: `TestResults/Actual/TC_001`
- `outputFilePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_`

```c#
kafkaHelper.ConsumeMessageFromKafka(topicName, consumerGroup, waitTimeInSeconds, offSetResetTime, Func, actualPath, outputFilePrefix);
```

- `topicName`: The topic from where you want to consume the message. Example: `js-logistics-inbound-v1-tst`
- `consumerGroup`: The consumer group from which you want to consume the messages. Example: `sainsburys.application.lsp.<replace>.intgtst`
- `safetyTimeoutSeconds`: A ceiling to avoid hanging indefinitely if a broker error or unexpected gap occurs. Example: `60`
- `offSetResetTime`: To return the available offsets after the time mentioned. Example: `DateTime.UtcNow`
- `Func`: A function to filter the consumed Kafka messages based on the filter conditions. The `Func<string, string, string>` delegate accepts a Kafka topic and a message as parameters and returns the serialized message as a string
- `actualPath`: Path where you want to store the consumed files. Example: `TestResults/Actual/TC_001`
- `outputFilePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_`

### SendMessageToKafka

```c#
await kafkaHelper.SendMessageToKafkaAsync(topicName, key, inputPath, headers);
```

- `topicName`: The topic to which you want to post the messages. Example: `js-logistics-inbound-v1-tst`
- `key`: The key to be used for the message. Example: `1345`
- `inputPath`: Path to the files you want to post. Example: `TestResults/Input/TC_001/`
- `headers`: A list of `IHeader` instances.

```c#
List<IHeader> headers = new List<IHeader>
{
		new KafkaHeader(Key = "headerKey", Value = "headerValue")
};
```

## KafkaAvroHelper

This class provides all the necessary methods to post messages to a Kafka Avro topic and to consume messages from a Kafka Avro topic.

### Initialization

```c#
KafkaAvroHelper<T> kafkaAvroHelper = new KafkaAvroHelper<T>(kafkaBroker, kafkaUsername, kafkaPassword, avroSchemaURL, avroSchemaAuth, logFilePath);
```

- `T`: Entity type that will be used for serializing/deserializing the message.
- `kafkaBroker`: The Kafka broker URL.
- `kafkaUsername`: The username for Kafka authentication.
- `kafkaPassword`: The password for Kafka authentication.
- `avroSchemaURL`: The URL of the Avro schema registry.
- `avroSchemaAuth`: The authentication token for the Avro schema registry.
- `logFilePath`: The file path for logging.

```c#
KafkaAvroHelper<T> kafkaAvroHelper = new KafkaAvroHelper<T>(kafkaBroker, saslOauthbearerConfig, avroSchemaURL, avroSchemaRegistryOauthBearerConfig, logFilePath);
```

- `T`: Entity type that will be used for serializing/deserializing the message.
- `kafkaBroker`: The Kafka broker URL.
- `saslOauthbearerConfig`: The Kafka OIDC configuration for Kafka authentication.
- `avroSchemaURL`: The URL of the Avro schema registry.
- `avroSchemaRegistryOauthBearerConfig`: The authentication token for the Avro schema registry.
- `logFilePath`: The file path for logging.

### ConsumeMessageFromKafka

```c#
kafkaAvroHelper.ConsumeMessageFromKafka(topicName, consumerGroup, waitTimeInSeconds, offSetResetTime, actualPath, outputFilePrefix);
```

- `topicName`: The topic from where you want to consume the message. Example: `js-logistics-inbound-v1-tst`
- `consumerGroup`: The consumer group from which you want to consume the messages. Example: `sainsburys.application.lsp.<replace>.intgtst`
- `waitTimeInSeconds`: The maximum elapsed time to fetch messages. Example: `60`
- `offSetResetTime`: To return the available offsets after the time mentioned. Example: `DateTime.UtcNow`
- `actualPath`: Path where you want to store the consumed files. Example: `TestResults/Actual/TC_001`
- `outputFilePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_`

```c#
kafkaHelper.ConsumeMessageFromKafka(topicName, consumerGroup, waitTimeInSeconds, offSetResetTime, Func, actualPath, outputFilePrefix);
```

- `topicName`: The topic from where you want to consume the message. Example: `js-logistics-inbound-v1-tst`
- `consumerGroup`: The consumer group from which you want to consume the messages. Example: `sainsburys.application.lsp.<replace>.intgtst`
- `waitTimeInSeconds`: The maximum elapsed time to fetch messages. Example: `60`
- `offSetResetTime`: To return the available offsets after the time mentioned. Example: `DateTime.UtcNow`
- `Func`: A function to filter the consumed Kafka messages based on the filter conditions. The `Func<string, T, string>` accepts a Kafka topic and a class delegate as parameters and returns the serialized message as a string
- `actualPath`: Path where you want to store the consumed files. Example: `TestResults/Actual/TC_001`
- `outputFilePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_`

### SendMessageToKafka

```c#
await kafkaAvroHelper.SendMessageToKafkaAsync(topicName, key, deserializedMessage);
```

- `topicName`: The topic to which you want to post the messages. Example: `js-logistics-inbound-v1-tst`
- `key`: The key to be used for the message. Example: `1345`
- `deserializedMessage`: The message you want to post to the Kafka topic. Example: `JsonSerializer.Deserialize<T>(File.ReadAllText(inputFile))`, where `T` is the input model class.

```c#
await kafkaAvroHelper.SendMessageToKafkaAsync(topicName, key, inputPath);
```

- `topicName`: The topic to which you want to post the messages. Example: `js-logistics-inbound-v1-tst`
- `key`: The key to be used for the message. Example: `1345`
- `inputPath`: Path to the files you want to post. Example: `TestResults/Input/TC_001/`

## MQHelper

This class provides methods to interact with an MQ (Message Queue).

### Initialization

```c#
MQOptions mQOptions = new()
{
		queueManagerName = "yourQueueManagerName",
		hostName = "yourHostName",
		portNumber = 1414,
		channelId = "yourChannelId",
		accessQueue = "yourAccessQueue"
};

MQHelper mQHelper = new(mQOptions, logFilePath);
```

- `queueManagerName`: The name of the queue manager.
- `hostName`: The name of the host.
- `portNumber`: The port number to connect to.
- `channelId`: The channel identifier.
- `accessQueue`: The access queue name.
- `logFilePath`: The path to the log file. Example: `TestResults/Logs/logFile.txt`

### PostMessagesToMQ

```c#
mQHelper.PostMessagesToMQ(inputPath, inputFilePrefix : null);
```

- `inputPath`: Path to the files you want to post. Example: `TestResults/Input/TC_001/`
- `inputFilePrefix` : An optional parameter to specify the prefix of the files to be posted. If not provided, all files in the specified path will be posted. Example: `InputFile_`

## ServiceBusHelper

This class provides all the necessary methods to interact with Azure Service Bus.

### Initialization

```c#
ServiceBusHelper serviceBusHelper = new(serviceBusConnectionString, logFilePath);
```

- `connectionString`: The connection string to the Azure Service Bus.
- `logFilePath`: The path to the log file.

### SendMessage

```c#
serviceBusHelper.SendMessageAsync(inputPath);
```

- `inputPath`: Path to the files you want to post to serivce bus queue. Example: `TestResults/Input/TC_001/`

## MongoHelper

This class provides all the necessary methods related to MongoDB (read Mongo data, delete Mongo data, read specific entry based on filter conditions, delete entry based on filter conditions, etc.).

### Pre-requisite when using MongoHelper methods

Set the Mongo conventions in the global setup method like a Hooks/Test Fixtures/Setup class as below:

```c#
MongoExtensions.SetConventions();
```

### Initialization

```c#
MongoHelper<T> mongoHelper = new(mongoConnectionString, mongoDbName, collectionName, logFilePath);
```

- `filter`: A filter created using the `Builders<T>.Filter` class to specify the criteria for selecting documents.
- `options`: An instance of `FindOptions<T, T>` that allows you to customize the behavior of the `Find` operation.

### DeleteAllDocumentsAsync

The `DeleteAllDocumentsAsync` method removes all records from the database, effectively clearing the entire dataset.

```c#
await mongoHelper.DeleteAllDocumentsAsync();
```

The `DeleteAllDocumentsAsync` method removes all records from the database that match the given filter criteria.

```c#
await mongoHelper.DeleteAllDocumentsAsync(filter);
```

### GetDocumentsAsync

The `GetDocumentAsync` method retrieves a single document from a MongoDB collection based on the specified filter.

```c#
await mongoHelper.GetDocumentAsync(filter);
```

The `GetDocumentAsync` method retrieves a single document from a MongoDB collection based on the specified filter and options.

```c#
await mongoHelper.GetDocumentAsync(filter, options);
```

### GetAllDocumentsAsync

The `GetAllDocumentsAsync` method retrieves all documents from a MongoDB collection.

```c#
await mongoHelper.GetAllDocumentsAsync(outputPath, filePrefix);
```

- `actualPath`: Path where you want to store the downloaded files. Example: `TestResults/Actual/TC_001`
- `filePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_

The `GetAllDocumentsAsync` method retrieves all documents from a MongoDB collection based on the specified projections using the findOptions parameter.

```c#
await mongoHelper.GetAllDocumentsAsync(outputPath, filePrefix, findOptions);
```

- `actualPath`: Path where you want to store the downloaded files. Example: `TestResults/Actual/TC_001`
- `filePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_
- `findOptions`: An instance of `FindOptions<T, T>` that allows you to customize the behavior of the `Find` operation.


The `GetAllDocumentsAsync` method retrieves all documents from a MongoDB collection based on the specified filter and options.

```c#
await mongoHelper.GetAllDocumentsAsync(outputPath, filePrefix, filter, options);
```

- `actualPath`: Path where you want to store the downloaded files. Example: `TestResults/Actual/TC_001`
- `filePrefix`: The prefix you want for the file name in `actualPath`. Example: `ActualFile_
- `filter`: A filter created using the `Builders<T>.Filter` class to specify the criteria for selecting documents.
- `options`: An instance of `FindOptions<T, T>` that allows you to customize the behavior of the `Find` operation.

## InsertDocumentsAsync

The `InsertDocumentsAsync` method inserts a list of records into the MongoDB collection.

```c#
await mongoHelper.InsertDocumentsAsync(inputMessage);
```

- `inputMessage`: A list of records to be inserted into the MongoDB collection. Each record should be of type `T`, where `T` is the type parameter specified when initializing the `MongoHelper<T>` instance.


```c#
await mongoHelper.InsertDocumentsAsync(inputPath, deserializeFunc);
```
- `inputPath`: The path to the folder containing the files to be inserted into the MongoDB collection. Each file should contain records of type `T`, where `T` is the type parameter specified when initializing the `MongoHelper<T>` instance.
- `deserializeFunc`: A function that takes a JSON string and returns an instance of type T1. This allows the method to deserialize different types of models dynamically

## OracleHelper

```c#
	OracleOptions oracleOptions = new()
	{
		// Pass the mongo connection string, mongo database name, mongo collection name, depot number and log file path
	};

	OracleHelper oracleHelper = new(oracleOptions);
```

### InsertDataAsync

The `InsertDataAsync` method inserts a list of records into the Oracle database.

```c#

	Dictionary<string, object> fields = new Dictionary<string, object>()
	{
		{ "fieldName", "fieldValue" },
		{ "fieldName2", "fieldValue2" }
	};
	
```

```c#
	await oracleHelper.InsertDataAsync(tableName, fields);
```

- `tableName`: The name of the table where you want to insert the records.
- `fields`: A dictionary containing the field names and their corresponding values to be inserted into the table.

### GetDataAsync

The `GetDataAsync` method retrieves records from the Oracle database based on the specified query.

```c#
	await oracleHelper.GetDataAsync(query, actualPath, actualFilePrefix);
```

- `query`: The SQL query to be executed.
- `actualPath`: The path where you want to store the retrieved records.
- `actualFilePrefix`: The prefix for the output file name.

## FileGenerationHelper

This class provides methods to generate files in different formats (CSV, XML, JSON) and to read files from a specified path.

### Initialization

```c#
FileGenerationHelper fileGenerationHelper = new FileGenerationHelper(testDataFilePath, logFilePath);
```

- `testDataFilePath`: The path to the test data file.
- `logFilePath`: The path to the log file.

- ### GenerateJsonFiles

```c#
fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName, InputData, outputPath, filePrefix, folderName);
```

- `scenario`: The scenario name for which the file is being generated.
- `workSheetName`: The name of the worksheet.
- `InputData`:  A function which sends scenario data in an array and returns the serialized json data to be written to the file.
- `outputPath`: The path where the file will be generated.
- `filePrefix`: The prefix for the file name.
- `folderName`: The name of the folder where the file will be generated.

```c#
fileGenerationHelper.GenerateJsonFiles(scenario, workSheetName, InputData, outputPath, folderName);
```

- `scenario`: The scenario name for which the file is being generated.
- `workSheetName`: The name of the worksheet.
- `InputData`:  A function which sends scenario data in an array and returns the serialized json data to be written to the file and 
                file prefix(string and string format) to be written to the file.
- `outputPath`: The path where the file will be generated.
- `folderName`: The name of the folder where the file will be generated.

### GenerateXmlFiles

```c#
fileGenerationHelper.GenerateXmlFiles(scenario, workSheetName, InputData, outputPath, filePrefix, folderName);
```

- `scenario`: The scenario name for which the file is being generated.
- `workSheetName`: The name of the worksheet.
- `InputData`: A function which sends scenario data in an array and returns the serialized xml data to be written to the file.
- `outputPath`: The path where the file will be generated.
- `filePrefix`: The prefix for the file name.
- `folderName`: The name of the folder where the file will be generated.

### GenerateFlatFiles

```c#
fileGenerationHelper.GenerateFlatFiles(scenario, workSheetName, InputData, outputPath, filePrefix, fileExtension, folderName);
```

- `scenario`: The scenario name for which the file is being generated.
- `workSheetName`: The name of the worksheet.
- `InputData`: A function which sends scenario data in an array and returns the flat file data(in strinbuilder format) to be written to the file. or
			   A function which sends scenario data in an array and returns the flat file data and file prefix(in stringbuilder and string format) to be written to the file
- `outputPath`: The path where the file will be generated.
- `filePrefix`: The prefix for the file name.
- `fileExtension`: The file extension (e.g., `.csv`, `.txt`, `.rec`).
- `folderName`: The name of the folder where the file will be generated.

### ReadExcelData

```c#
fileGenerationHelper.ReadExcelData(scenario, workSheetName);
```

- `scenario`: The scenario name for which the file is being read.
- `workSheetName`: The name of the worksheet.


### GenerateExcelFiles

```c#
fileGenerationHelper.GenerateExcelFiles(scenario, testDataWorkSheetName, expectedWorkSheetName, outputPath, filePrefix, headers);
```

- `scenario`: The scenario name for which the file is being generated.
- `testDataWorkSheetName`: The name of the worksheet in the test data file.
- `expectedWorkSheetName`: The name of the worksheet in the expected data file.
- `outputPath`: The path where the file will be generated.
- `filePrefix`: The prefix for the file name.
- `headers`: A list of headers to be included in the Excel file.

```c#
fileGenerationHelper.GenerateExcelFiles(scenario, testDataWorkSheetName, expectedWorkSheetName, InputData, outputPath, headers, mergedHeaderMap);
```

- `scenario`: The scenario name for which the file is being generated.
- `testDataWorkSheetName`: The name of the worksheet in the test data file.
- `expectedWorkSheetName`: The name of the worksheet in the expected data file.
- `InputData`: A function which sends scenario data in an array and returns the file prefix.
- `outputPath`: The path where the file will be generated.
- `headers`: A list of headers to be included in the Excel file.
- `mergedHeaderMap`: A dictionary that maps merged headers to their corresponding individual headers.

## FlatFileComparisonHelper

This class provides methods to compare flat files (CSV, TEXT, REC etc.)

### Initialization

```c#
FileComparisonHelper fileComparisonHelper = new FileComparisonHelper(expectedFilesPath, actualFilesPath, filePrefix, logFilePath, errorMessage : null,componentName : null);
```
 
- `expectedFilesPath`: The path to the expected files.
- `actualFilesPath`: The path to the actual files.
- `filePrefix`: The prefix for the files to be compared.
- `logFilePath`: The path to the log file.
- `errorMessage`: An optional parameter to specify a custom error message to be logged in case of a comparison failure. If not provided, a default error message will be logged.
- `componentName`: An optional parameter to specify the component name for logging purposes. If not provided, it will be assumed that the comparison is being done for a generic component.

### CompareFlatFiles

```c#
fileComparisonHelper.CompareFlatFiles(contentDefinitions);
```

- `contentDefinitions`: A list of `ContentDefinition` objects that define the structure and content of the files to be compared.

```c#
fileComparisonHelper.CompareFlatFiles(headerDefinition, contentDefinitions, trailerDefinition);
```

- `headerDefinition`: A `HeaderDefinition` object that defines the structure of the header in the files.
- `contentDefinitions`: A list of `ContentDefinition` objects that define the structure and content of the files to be compared.
- `trailerDefinition`: A `TrailerDefinition` object that defines the structure of the trailer in the files.

### CompareDelimitedFiles

```c#
fileComparisonHelper.CompareDelimitedFiles(keyColumnIndex, ignoreFieldIndices, fileExtensions: fileExtensions);
```

 - `keyColumnIndex`: The zero-based column index used as the key for comparison.
 - `ignoreColumnIndices`: A list of column indices to be ignored during the comparison.
 - `comparisonFiles`: (Optional) A label used in log output for this comparison.
 - `fileExtensions`: (Optional) A list of file extensions (e.g. `"csv"`, `"txt"`, `"dat"`) to include; defaults to `"csv"`

### CompareExcelFiles

```c#
fileComparisonHelper.CompareExcelFiles(expectedFilePath, actualFilePath, filePrefix, comparisonFiles, keyColumnName, ignoreColumnNames);
```

- `expectedFilePath`: The path to the expected Excel file.
- `actualFilePath`: The path to the actual Excel file.
- `filePrefix`: The prefix for the files to be compared.
- `comparisonFiles`: The workSheet name of the expected data for logging.
- `keyColumnName`: The name of the column to be used as a key for comparison.
- `ignoreColumnNames`: A list of column names to be ignored during the comparison(optional).

### CompareJsonFiles

```c#
fileComparisonHelper.CompareJsonFiles<T>(comparisonFiles, Func<T, Object> keySelector);
```

- `comparisonFiles`: The name to be used for logging the comparison results.
- `keySelector`: A function that takes an object of type `T` and `Object` for Key selector returns a key of any datatype to be used for comparison. This allows the method to compare JSON files based on a specific key extracted from the JSON objects.

## EncryptionHelper

This class provides methods to encrypt strings using AES encryption.

### EncryptString

```c#
string encryptedString = EncryptionHelper.EncryptString(encryptionKey, plainText);
```

- `encryptionKey`: The key used for encryption. It should be a 16, 24, or 32-byte string.
- `plainText`: The string to be encrypted.

## HtmlReportHelper

This class provides methods to generate HTML reports for test results.

### GenerateTestResultsHtmlReport
```c#
HtmlReportHelper.GenerateTestResultsHtmlReport(testsExecutionStartTime,totalTestsExecutionEndTime, serviceName, scenarioResults, reportPath, config: null);
```
- `testsExecutionStartTime`: The start time of the test execution.
- `totalTestsExecutionEndTime`: The end time of the test execution.
- `serviceName`: The name of the service for which the report is being generated.
- `scenarioResults`: A list of `ScenarioResult` objects that contain the results of each scenario executed.
- `reportPath`: The path where the HTML report will be generated.
- `config`: An optional object of type `Dictionary<string, bool>` that defines which component test runs are skipped or executed. The key is the component name and the value is a boolean where `true` indicates that the test run for that component was executed and `false` indicates that it was skipped. If not provided, it will be assumed that all components were executed and none were skipped.
## EmailHelper

This class provides methods to send emails with report files from LSP Common mail to recipients.

### SendEmail
```c#
EmailHelper.SendEmail(recipients, subject, htmlReportFilePath, scenarioResults)
```
- `recipients`: A list of email addresses to which the email will be sent.
- `subject`: The subject of the email.
- `htmlReportFilePath`: The directory path containing the HTML report file(s) to be attached in the email.
- `scenarioResults`: A list of `ScenarioResult` objects that contain the results of each scenario executed, which can be used to customize the email content based on the test results.