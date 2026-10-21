using LogisticsPlatform.Testing.Extensions.Models;
using Microsoft.Identity.Client;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class ApiHelper(ApiOptions apiOptions)
{
    private readonly HttpClient _client = new();
    
    public async Task<HttpResponseMessage> PostMessageAsync(string url, string content, bool authRequired, Dictionary<string, string> headers = null)
    {
        await SetHeaders(authRequired, headers);

        return await _client.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
    }

    public async Task PostMessageAsync(string apiUrl, string inputPath, string actualPathWithPrefix = "", bool authRequired = true, Dictionary<string, string> headers = null)
    {
        FileHelper.AppendLine(apiOptions.LogFilePath, "Posting Messages to Api");
        await SetHeaders(authRequired, headers);
        int count = 1;
        string[] allFiles = FileHelper.GetAllFiles(inputPath);
        foreach (string filePath in allFiles)
        {
            HttpResponseMessage response = null;
            var detectedContentType = GetContentType(filePath);
            if (string.Equals(detectedContentType, "text/plain", StringComparison.OrdinalIgnoreCase))
            {
                var content = new StringContent(FileHelper.GetAllText(filePath));
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                response = await _client.PostAsync(apiUrl, content);
            }
            else
            {
                response = await _client.PostAsync(apiUrl, new StringContent(FileHelper.GetAllText(filePath), Encoding.UTF8, detectedContentType));
            }
                
            if (response.IsSuccessStatusCode)
            {
                string logFilePath = apiOptions.LogFilePath;
                FileHelper.AppendLine(logFilePath, $"Received {response.StatusCode} while posting message to API.");
                string actualFilePath = actualPathWithPrefix + count++ + ".json";
                FileHelper.WriteLine(actualFilePath, await response.Content.ReadAsStringAsync());
            }
            else if (actualPathWithPrefix != null && response.StatusCode == HttpStatusCode.BadRequest)
            {
                string logFilePath = apiOptions.LogFilePath;
                object arg = (int)response.StatusCode;
                FileHelper.AppendLine(logFilePath, $"Received {arg} while posting message to API. Error: {await response.Content.ReadAsStringAsync()}");
                string actualFilePath = actualPathWithPrefix + count++ + ".json";
                FileHelper.WriteLine(actualFilePath, await response.Content.ReadAsStringAsync());
            }
            else
            {
                string logFilePath = apiOptions.LogFilePath;
                object arg = (int)response.StatusCode;
                FileHelper.AppendLine(logFilePath, $"Received {arg} while posting message to API. Error: {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    public async Task GetMessageAsync(string apiUrl, string actualPath, string outputFilePrefix = "", bool authRequired = true, Dictionary<string, string> headers = null)
    {
        FileHelper.AppendLine(apiOptions.LogFilePath, "Getting Message from an Api");

        await SetHeaders(authRequired, headers);

        var response = await _client.GetAsync(apiUrl);
        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            FileHelper.AppendLine(apiOptions.LogFilePath, $"Received {(int)response.StatusCode} while getting message from API.");
            FileHelper.WriteLine($"{actualPath}/{outputFilePrefix}.json", await response.Content.ReadAsStringAsync());
        }        
        else
        {
            FileHelper.AppendLine(apiOptions.LogFilePath, $"Received {(int)response.StatusCode} while getting message from API. Error: {await response.Content.ReadAsStringAsync()}");
        }
    }

    public async Task<HttpResponseMessage> GetMessageAsync(string apiUrl, bool authRequired, Dictionary<string, string> headers = null)
    {
        FileHelper.AppendLine(apiOptions.LogFilePath, "Getting Message from an Api");

        await SetHeaders(authRequired, headers);

        return await _client.GetAsync(apiUrl);
    }

    private async Task SetHeaders(bool authRequired, Dictionary<string, string> headers)
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue( "*/*"));

        if (headers != null)
        {
            foreach (var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        if (authRequired)
            await SetAuthentication();
    }

    private async Task SetAuthentication()
    {
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(apiOptions.ApiClientId)
                .WithClientSecret(apiOptions.ApiClientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{apiOptions.ApiTenantId}/oauth2/v2.0/token"))
                .Build();

        AuthenticationResult result = await app.AcquireTokenForClient([apiOptions.ApiResourceId + "/.default"])
            .ExecuteAsync();

        if (result != null)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        }
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
