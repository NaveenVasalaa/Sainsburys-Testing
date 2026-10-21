namespace LogisticsPlatform.Testing.Extensions.Models;

public class MQOptions
{
    public string HostName { get; set; }
    public string PortNumber { get; set; }
    public string ChannelId { get; set; }
    public string QueueManager { get; set; }
    public string AccessQueue { get; set; }
    public string DetailQueueName { get; set; }
    public short TimeOutSeconds { get; set; } = 310;
    public short MaxRetries { get; set; } = 3;
    public short RetryDelaySeconds { get; set; } = 10;
}
