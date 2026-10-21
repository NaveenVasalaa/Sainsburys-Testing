using Azure.Messaging.ServiceBus;
using MassTransit;
using System.Text.Json;

namespace LogisticsPlatform.Testing.Extensions.Helpers
{
    public class ServiceBusHelper<T> where T : class
    {
        private readonly IBusControl _bus;
        public readonly string _logFilePath;

        public ServiceBusHelper(string serviceBusConnectionString, string logFilePath)
        {
            _bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(serviceBusConnectionString, configurator =>
                {
                    configurator.TransportType = ServiceBusTransportType.AmqpTcp;
                    TimeSpan.FromSeconds(30);
                });
            });
            _logFilePath = logFilePath;
        }

        public async Task SendMessageAsync(string inputPath)
        {
            try
            {
                var inputFiles = FileHelper.GetAllFiles(inputPath);

                foreach (var inputFile in inputFiles)
                {
                    await _bus.StartAsync();
                    await _bus.Publish(JsonSerializer.Deserialize<T>(File.ReadAllText(inputFile)));
                    await Task.Delay(500, new CancellationToken(default));
                    FileHelper.AppendLine(_logFilePath, $"Message(s) posted successfully");
                }
            }
            catch (Exception ex)
            {
                FileHelper.AppendLine(_logFilePath, $"Error while posting to service bus: {ex.Message}");
            }
        }
    }
}
