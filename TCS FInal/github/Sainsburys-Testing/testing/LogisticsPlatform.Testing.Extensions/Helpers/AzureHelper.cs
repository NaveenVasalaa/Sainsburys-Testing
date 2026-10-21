using Azure.Security.KeyVault.Keys;
using LogisticsPlatform.AzureConnector.Component;
using LogisticsPlatform.AzureConnector.Component.DataModel;
using System.Diagnostics;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class AzureHelper(string blobConnectionString, string containerName, string logFilePath)
{
    private readonly IAzureSubscriber _azureSubscriber = new AzureSubscriber(new AzureStorageProcess());

    #region Blob
    /// <summary>
    /// Check If the File specified is present or not in the Blob Storage
    /// </summary>
    /// <param name="fileNameWithPath"></param>
    /// <returns></returns>
    public async Task<bool> CheckIfFileExistsAsync(string fileNameWithPath)
    {
        try
        {
            if (await _azureSubscriber.FileExists(new AzureRequest
            {
                SourceContainerName = containerName,
                SourceFileName = fileNameWithPath,
                SourceKey = blobConnectionString,
                MaxRetryAttempts = 3
            }))
            {
                return true;
            }
            else
            {
                FileHelper.AppendLine(logFilePath, $" File Not Found in Azure Blob Storage Container: {containerName}, File: {fileNameWithPath}");
            }
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(logFilePath, $"Error occurred while Getting the Files lists from the Container: {containerName} FolderPath: {fileNameWithPath}, {ex}");
        }

        return false;
    }

    /// <summary>
    /// Check If the Files with Prefix specified is present or not in the Blob Storage
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <param name="filePrefix"></param>
    /// <param name="waitTimeInSeconds"></param>
    /// <returns></returns>
    public async Task<bool> CheckIfFileExistsAsync(string blobFilePath, List<string> filePrefix, int waitTimeInSeconds)
    {
        try
        {
            var watch = Stopwatch.StartNew();
            int elapsedSeconds = 0;
            while (elapsedSeconds < waitTimeInSeconds)
            {
                if (await _azureSubscriber.CheckFileExistsInFolder(new AzureRequest
                {
                    SourceContainerName = containerName,
                    SourceFolderName = blobFilePath.TrimEnd('/') + "/" + filePrefix.First(),
                    SourceKey = blobConnectionString,
                    MaxRetryAttempts = 3
                }))
                {
                    FileHelper.AppendLine(logFilePath, $" File Found in Azure Blob Storage Container: {containerName}, File Path: {blobFilePath}");
                    return true;
                }

                elapsedSeconds = Convert.ToInt32(watch.Elapsed.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(logFilePath, $"Error occurred while Getting the Files lists from the Container: {containerName} FolderPath: {blobFilePath}, {ex}");
        }

        FileHelper.AppendLine(logFilePath, $" File Not Found in Azure Blob Storage Container: {containerName}, File Path: {blobFilePath}");
        return false;
    }

    /// <summary>
    /// Write the string content to the Azure BLob in specified folder details
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <param name="inputPath"></param>
    /// <returns></returns>
    public async Task PutFileInAzureAsync(string blobFilePath, string inputPath)
    {
        FileHelper.AppendLine(logFilePath, "Uploading Files in Blob");
        try
        {
            var inputFiles = FileHelper.GetAllFiles(inputPath);

            foreach (var inputFile in inputFiles)
            {
                bool response = await _azureSubscriber.PutFileInToAzureContainer(new AzureRequest
                {
                    BodyContent = File.ReadAllText(inputFile),
                    MaxRetryAttempts = 3,
                    SourceContainerName = containerName,
                    SourceFileName = blobFilePath.TrimEnd('/') + "/" + Path.GetFileName(inputFile),
                    SourceKey = blobConnectionString
                });

                if (response)
                {
                    FileHelper.AppendLine(logFilePath, $"Azure Blob File writing process succeeded, Container: {containerName}, File: {blobFilePath}");
                }
                else
                {
                    new Exception($"Azure Connector Library for Put File returned False. Container: {containerName}. File: {blobFilePath}");
                }
            }
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(logFilePath, $"Azure Blob file writing process Error, Container: {containerName}. File: {blobFilePath}, {ex}");
        }
    }

    /// <summary>
    /// Write the stream content to the Azure BLob in specified folder details
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <param name="inputPath"></param>
    /// <returns></returns>
    public async Task PutStreamInAzureAsync(string blobFilePath, string inputPath)
    {
        FileHelper.AppendLine(logFilePath, "Uploading Files in Blob");
        try
        {
            var inputFiles = FileHelper.GetAllFiles(inputPath);

            foreach (var inputFile in inputFiles)
            {
                using var fileStream = File.OpenRead(inputFile); // binary-safe

                bool response = await _azureSubscriber.PutStreamInToAzureContainer(new AzureRequest
                {
                    Stream = fileStream, // ADD THIS PROPERTY TO AzureRequest IF NOT PRESENT
                    MaxRetryAttempts = 3,
                    SourceContainerName = containerName,
                    SourceFileName = $"{blobFilePath.TrimEnd('/')}/{Path.GetFileName(inputFile)}",
                    SourceKey = blobConnectionString
                });

                if (response)
                    FileHelper.AppendLine(logFilePath, $"Azure Blob stream upload succeeded. Container: {containerName}, File: {inputFile}");
                else
                    FileHelper.AppendLine(logFilePath, $"Azure Blob stream upload failed. Container: {containerName}, File: {inputFile}");
            }
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(logFilePath, $"Azure Blob stream upload error. Container: {containerName}. Path: {blobFilePath}, {ex}");
        }
    }

    /// <summary>
    /// Download File from blob
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <param name="actualPath"></param>
    /// <param name="filePrefix"></param>
    /// <param name="waitTimeInSeconds"></param>
    /// <returns></returns>
    public async Task DownloadFilesFromAzureBlobAsync(string blobFilePath, string actualPath, List<string> filePrefix, int waitTimeInSeconds, string encryptionKeyVaultName = null, string encryptionKeyName = null)
    {
        try
        {
            FileHelper.AppendLine(logFilePath, "Downloading Files in Blob");
            bool isFileExists = await CheckIfFileExistsAsync(blobFilePath, filePrefix, waitTimeInSeconds);
            if (isFileExists)
            {
                IEnumerable<string> fileNames = _azureSubscriber.GetAllFileNamesByAzureStorageBlob(new AzureRequest
                {
                    SourceKey = blobConnectionString,
                    SourceContainerName = containerName,
                    SourceFolderName = blobFilePath.TrimEnd('/'),
                    FilePrefix = filePrefix
                });

                if (!fileNames.Any())
                    throw new Exception("No files found in the Blob");
                else
                {
                    int lastIndex = fileNames.FirstOrDefault().LastIndexOf("/");
                    foreach (var fileName in fileNames)
                    {
                        string file = fileName.Substring(lastIndex + 1);

                        var stream = await _azureSubscriber.DownloadFileFromBlob(new AzureRequest
                        {
                            SourceContainerName = containerName,
                            SourceFileName = blobFilePath.TrimEnd('/') + "/" + file,
                            SourceKey = blobConnectionString,
                            EncryptionKeyVaultName = encryptionKeyVaultName,
                            EncryptionKeyName = encryptionKeyName
                        });
                        using (var fileStream = new FileStream(actualPath.TrimEnd('/') + "/" + file, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                        }

                        FileHelper.AppendLine(logFilePath, $"Downloaded file {file} from the Blob");
                    }
                }
            }
            else
            {
                FileHelper.AppendLine(logFilePath, $"File not found in the Blob Storage Container: {containerName}, File Path: {blobFilePath}");
            }
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(logFilePath, $"Error occurred while downloading the files from the Blob, {ex}");
        }
    }

    /// <summary>
    /// Delete File from blob
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <param name="filePrefix"></param>
    /// <returns></returns>
    public async Task DeleteFilesinAzureBlobAsync(string blobFilePath, List<string> filePrefix)
    {
        try
        {
            IEnumerable<string> fileNames = _azureSubscriber.GetAllFileNamesByAzureStorageBlob(new AzureRequest
            {
                SourceKey = blobConnectionString,
                SourceContainerName = containerName,
                SourceFolderName = blobFilePath.TrimEnd('/'),
                FilePrefix = filePrefix
            });

            if (!fileNames.Any())
                FileHelper.AppendLine(logFilePath, "No files found in the Blob");
            else
            {
                int lastIndex = fileNames.FirstOrDefault().LastIndexOf("/");
                foreach (var fileName in fileNames)
                {
                    string file = fileName[(lastIndex + 1)..];

                    await _azureSubscriber.DeleteFileFromBlob(new AzureRequest
                    {
                        SourceContainerName = containerName,
                        SourceKey = blobConnectionString,
                        SourceFileName = blobFilePath + "/" + file
                    });

                    FileHelper.AppendLine(logFilePath, $"Deleted file {file} from the Blob");
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Delete All Files from blob
    /// </summary>
    /// <param name="blobFilePath"></param>
    /// <returns></returns>
    public async Task DeleteAllFilesinAzureBlobAsync(string blobFilePath)
    {
        FileHelper.AppendLine(logFilePath, "Deleting Files in Blob");
        try
        {
            await _azureSubscriber.DeleteAllFilesFromBlob(new AzureRequest
            {
                SourceContainerName = containerName,
                SourceKey = blobConnectionString,
                SourceFolderName = blobFilePath
            });

            FileHelper.AppendLine(logFilePath, $"Deleted files from the Blob {blobFilePath}");
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region KeyVault

    public static string GetSecret(string keyVaultName, string secretKey)
    {
        var keyVaultSubscriber = new KeyVaultSubscriber();
        var secretRequest = new SecretRequest
        {
            KeyVaultName = keyVaultName,
            SecretKey = secretKey,
            TenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? string.Empty,
            ClientId = Environment.GetEnvironmentVariable("AZ_CLIENT_ID") ?? string.Empty,
            ClientSecret = Environment.GetEnvironmentVariable("AZ_CLIENT_SECRET") ?? string.Empty,
            MaxRetryAttempts = 3
        };

        return keyVaultSubscriber.GetSecret(secretRequest);
    }

    public static KeyVaultKey GetKey(string keyVaultName, string keyName)
    {
        var keyVaultSubscriber = new KeyVaultSubscriber();
        var keyRequest = new KeyRequest
        {
            KeyVaultName = keyVaultName,
            KeyName = keyName,
            TenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? string.Empty,
            ClientId = Environment.GetEnvironmentVariable("AZ_CLIENT_ID") ?? string.Empty,
            ClientSecret = Environment.GetEnvironmentVariable("AZ_CLIENT_SECRET") ?? string.Empty,
            MaxRetryAttempts = 3
        };

        return keyVaultSubscriber.GetKey(keyRequest);
    }
    #endregion
}
