# Release Notes

## 8.2.17
	- Updated ApiHelper.PostMessageAsync to ensure .txt payloads are sent with the exact Content-Type: text/plain header (no charset=utf-8). This resolves API validation failures (Error Code 101) where the target API rejects text/plain; charset=utf-8

## 8.2.16
	- Added `CompareDelimitedFiles` to allow comparing comma-delimited files across multiple extensions (e.g. `.csv`, `.txt`, `.dat`), and marked `CompareCsvFiles` obsolete while keeping it as a backward-compatible wrapper

## 8.2.15
    - Updated ConsumeUntilEnd method to allow the connection to stabilize before querying watermarks for OAuthBearer in KafkaAvroHelper class. This is to address the issue of getting "The connection has not been established" error when trying to consume messages from Kafka with OAuth authentication, as it allows the connection to be properly established and stabilized before attempting to consume messages.
	- Removed the NPOI library as it now requires a license. The library was previously used only for refreshing Excel files and was not utilized by most tests.

## 8.2.14
	- Updated DownloadFilesFromAzureBlobAsync method in AzureHelper class to include optional parameters encryptionKeyVaultName and encryptionKeyName to support downloading and decrypting files from Azure Blob Storage that are encrypted using Azure Key Vault.

## 8.2.13
	- Updated packages LogisticsPlatform.Common.Extensions 

## 8.2.12
    - Added code for capturing success response and write it to output file in PostMessageAsync method in ApiHelper class. Previously, only error responses were being captured and written to output file, now both success and error responses will be captured for better analysis and debugging of Api responses in test scenarios.
	- Updated package Microsoft.NET.Test.Sdk to the latest version - 18.5.1.

## 8.2.11
    - Updated System.Security.Cryptography.Xml from version 8.0.2 to 10.0.7 to address a vulnerability in version 8.0.2.

## 8.2.10
    - Update ApiHelper class PostMessageAsync method to send text/csv content type for csv files. previously it was sending application/json content type which was causing issues in some scenarios where the API was expecting text/csv content type for csv files.
	- Added a method to detect the content type based on the file extension and set the content type accordingly in PostMessageAsync method in ApiHelper class
	- Updated packages logisticsplatform.common.extensions, Microsoft.NET.Test.Sdk, xunit.v3.xunit.runner.visualstudio.

## 8.2.9
    - Updated BaseController class PostMessagesToMQ by adding an optional parameter i.e, fileprefix to specify the prefix of the files to be posted. If not provided, all files in the specified path will be posted
    - Updated Package dependencies: updated Microsoft.NET.Test.Sdk, xunit.v3, MassTransit.Azure.ServiceBus.Core, and NPOI

## 8.2.8
    - Updated string ErrorMessage field in ScenarioResult Model StringBuilder ErrorMessage to log multiple error messages to Append all error messages instead of overwriting with the latest error message in case of multiple errors in a scenario
	- Updated FileComparisonHelper class by Adding two optional parameters i.e, componentName & errorMessage for better logging of errors in case of file comparison failure and updated all the methods in FileComparisonHelper class to include these parameters
	- Updated FileComparisonHelper class CompareJsonFiles KeySelector field from string to object to support dynamic key selection based on the type of json file being compared
	- Updated PostMessagesToMQ by adding an optional parameter i.e, fileprefix to specify the prefix of the files to be posted. If not provided, all files in the specified path will be posted
	- Modified code in GenerateTestResultsHtmlReport Method in HtmlReportHelper class
	- Updated KafkaHelper and KafkaAvroHelper class to stop tracking Once a partition's last known message is consumed
	- Updated ResultExtensions class with StoreResult overload method to include exception message in the final result log for better debugging and analysis of test failures
	- Updated VerifyNoOutput method to return boolean value indicating whether there are no output files instead of throwing an exception 
	- Added String Component field to ScenarioResult Model to log the component name in case of file comparison failure for better analysis and debugging 

## 8.2.7
   	- Updated to consider only comma delimeter in CompareCsvFiles method instead of most occuring delimeter as we only support comma-delimited files
   	- Added CompareJsonFiles Method to compare json output files based on key

## 8.2.6
   	- Added HtmlReportHelper class, which provides the GenerateTestResultsHtmlReport method to generate HTML test result reports
   	- Added EmailHelper class, which provides SendEmail methods to email report files from the LSP Common mailbox to recipients

## 8.2.5
	- Added OAuth Configuration in KafkaHelper and KafkaAvroHelper class to support OAuth authentication for Kafka
	- Updated CompareFlatFiles method to compare files based on key, irrespective of order and log extra fields in actual file 

## 8.2.4
   	- Fixed bug in CompareFlatFiles where actualFileLines.Length was incorrectly using expectedFileLines.Length (line 210)
   	- Improved performance in CompareExcelFilesByKeyColumnName by using TryGetValue pattern instead of ContainsKey
   	- Updated package dependencies: added CsvHelper; updated Microsoft.NET.Test.Sdk, xunit.v3, MassTransit.Azure.ServiceBus.Core, and LogisticsPlatform.Common.Extensions
   	- Updated CompareCsvFiles method logic to compare content based on key irrespective of order
   	- Added Helper method in OracleHelper class to set time zone to UTC to fix ORA-01882: timezone region not found error in pipeline
   
## 8.2.3
    - Updated ConsumeMessageFromKafka in KafkaAvroHelper with a Function accepts a Kafka topic and a class delegate as parameters and returns the serialized message as a string

## 8.2.2
   	- Added overload method ConsumeMessageFromKafka in KafkaAvroHelper class to accept a Func to return kafka messages based on filter
   	- Updated CompareExcelFiles method to look for header in first 4 rows instead of just first row and compare data accordingly
   	- Updated ExcelFileGeneration methods in FileGenerationHelper class to merge header cells
   	- Updated CompareCsvFiles method to compare content based on key irrespective of order

## 8.2.1
    - Added overload method ConsumeMessageFromKafka to accept a Func to return kafka messages based on filter
    - Added PutStreamInAzureAsync method in AzureHelper class

## 8.1.9
	- Added ExcelFileGeneration methods in FileGenerationHelper class to generate excel files from datatable
	- Added EncryptionHelper class to encrypt strings using AES encryption
	- Updated FileComparisonHelper class to sort and compare files

## 8.1.8

	- Included GetMessageAsync method to store 400 error in output file

## 8.1.7

	- Fixed MQHelper PostMessagetoMQ method to get all lines instead of entire message
	- Fixed ORA-50045: Invalid operation on a closed object error in ReadDataAndStoreInFileAsync method of OracleHelper class

## 8.1.6

	- Fixed Checkmarx vulnerability found in FileHelper

## 8.1.5

	- Updated BaseController class method for sending messages to Kafka with headers

## 8.1.4

	- Renamed FlatFileComparisonHelper class to FileComparisonHelper
	- Added CompareExcelFiles method to FileComparisonHelper class
	- Updated InsertDocumentsAsync method in MongoHelper class with a function which returns an instance of type T1

## 8.1.3

	- Added overload method GenerateJsonFiles to return json string and filePrefix

## 8.1.2

	- Added InsertDocumentsAsync overload method to accept input message
	- Added SendMessageToKafkaAsync overload method to accept input message

## 8.1.1

	- Added a parameter to pass headers in BaseController PostMesageToApi methods
	- Updated SendMessageToKafkaAsync method for avro to accept input message instead of inputfolder

## 8.1.0

    - Added a parameter to pass headers to ApiHelper methods
	- Updated FlatFileComparisonHelper comparison methods to log extra fields in actual file

## 8.0.9

	- Added methods in MongoHelper class to insert, delete and download documents
	- Update methods in FlatFileComparisonHelper class to return boolean value

## 8.0.8

    - Added BaseController class
	- Renamed methods in ApiHelper class with Async suffix
	- Updated ServiceBusHelper class method to get input file path and post message to Service Bus
	- Updated README.md with above mentioned changes

## 8.0.7

	- Added logging in FileGenerationHelper class
	- Updated logging in FlatFileComparisonHelper class

## 8.0.6

	- Added OracleHelper class to connect to Oracle DB and insert and retrieve data
	- Updated KafkaHelper class to retrieve available offsets based on the time provided and consume messages from these available offsets
	- Added methods in FileGenerationHelper class to generate xml and flat files
	- Added FlatFileComparisonHelper class with methods to compare flat files
	- Updated README.md with instructions on how to use the library

## 8.0.5
	- Added ExcelExtensions class with a method to refresh excel file using NPOI library
	- Added ResultExtensions class to store and update final result in log file
	- Added TestContextExtensions class to return TestMethodArguments
	- Added FileGenerationHelper class to read excel data and generate files
	- Removed ApiResponseStorageExtension class
	- Updated ApiHelper class to log ApiReponse status and store response in json file for BadRequest
	- Updated MongoHelper class with Delete and GetAllDocuments methods to delete and store all documents from MongoDB collection in json file
	- Updated AzureHelper class with new methods to check if file exists in blob based on file prefix and to delete all files in blob
	- Added JsonSerializerExtensions class to serialize and deserialize objects to/from JSON
	- Updated README.md with above mentioned changes

## 8.0.4
	- Added DateTimeOffsetBsonSerializer in MongoExtensions class
	- Updated MongoHelper class to support MONGODB-OIDC authentication
	- Added GetMessageFromApiAsync method in ApiHelper class
	- Renamed Async methods with Async suffix
	- Updated ReadMe.md with new methods added in the library

## 8.0.3
	- Updated FileHelper class to create list of directories
	- Created ApiResponseStorageExtension class to retireve Api Response messages

## 8.0.2
	- Updated AzureHelper class to use methods from AzureConnector Component
	- Created MongoExtensions class to Set Conventions
	- Updated README.md with instrucions on how to use the library
	- Updated ApiHelper class with ApiOptions

## 8.0.1
	- Initial version of common libary for integration tests