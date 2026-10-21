using Confluent.Kafka;
using LogisticsPlatform.Common.Extensions.Kafka;
using LogisticsPlatform.Common.Extensions.Kafka.Enums;
using LogisticsPlatform.Common.Extensions.Kafka.Settings;
using System.Diagnostics;
using KafkaExtensions = LogisticsPlatform.Testing.Extensions.Extensions.KafkaExtensions;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class KafkaHelper
{
    private readonly string _broker;
    private readonly string _username;
    private readonly string _password;
    private readonly string _saslOauthbearerConfig;
    private readonly string _logFilePath;
    private readonly KafkaSettingsBase _producerSettings;
    private readonly KafkaService _kafkaService = new();

    /// <summary>
    /// Constructor for plain SASL (username/password) authentication.
    /// </summary>
    public KafkaHelper(string broker, string username, string password, string logFilePath)
    {
        _broker = broker;
        _username = username;
        _password = password;
        _saslOauthbearerConfig = null;
        _logFilePath = logFilePath;

        _producerSettings = new KafkaSettingsBase
        {
            Brokers = broker,
            UserName = username,
            Password = password
        };
    }

    /// <summary>
    /// Constructor for OAuthBearer authentication.
    /// saslOauthbearerConfig = Kafka broker OAuth config  "logicalCluster=lkc-xxxxx&scope=...&identityPool=..."
    /// </summary>
    public KafkaHelper(string broker, string saslOauthbearerConfig, string logFilePath)
    {
        _broker = broker;
        _username = null;
        _password = null;
        _saslOauthbearerConfig = saslOauthbearerConfig;
        _logFilePath = logFilePath;

        _producerSettings = new KafkaSettingsBase
        {
            Brokers = broker,
            SaslOauthBearerConfig = saslOauthbearerConfig,
            KafkaAuthType = KafkaAuthType.OauthConfig
        };
    }

    public async Task SendMessageToKafkaAsync(string topic, string key, string inputPath, IEnumerable<IHeader> headers = null)
    {
        _producerSettings.TopicName = topic;

        var inputFiles = FileHelper.GetAllFiles(inputPath);
        foreach (var inputFile in inputFiles)
        {
            var value = FileHelper.GetAllText(inputFile);
            var result = await _kafkaService.PublishToKafkaAsync(_producerSettings, topic, key, value, customHeaders: headers).ConfigureAwait(false);
            LogProduceResult(topic, result.Response, result.ExceptionMessage);
        }
    }

    public async Task SendMessageAsync(string topic, string key, string value, IEnumerable<IHeader> headers = null)
    {
        _producerSettings.TopicName = topic;
        var result = await _kafkaService.PublishToKafkaAsync(_producerSettings, topic, key, value, customHeaders: headers).ConfigureAwait(false);
        LogProduceResult(topic, result.Response, result.ExceptionMessage);
    }

    private void LogProduceResult(string topic, bool success, string errorMessage)
    {
        if (!success)
            FileHelper.AppendLine(_logFilePath, $"Error while publishing message to Kafka topic '{topic}': {errorMessage}");
        else
            FileHelper.AppendLine(_logFilePath, $"Message published to {topic}");
    }

    public void ConsumeMessageFromKafka(
        string topic,
        string consumerGroup,
        int safetyTimeoutSeconds,
        DateTime offSetResetTime,
        string actualPath,
        string actualFilePrefix = "")
    {
        int count = FileHelper.GetNextFileCount(actualPath, actualFilePrefix);

        using var consumer = BuildConsumer(consumerGroup);

        var startOffsets = GetOffsets(topic, consumerGroup, offSetResetTime, consumer);
        LogAndAssignOffsets(topic, startOffsets, consumer);

        ConsumeUntilEnd(consumer, startOffsets, safetyTimeoutSeconds, msg =>
        {
            if (!string.IsNullOrEmpty(actualPath))
            {
                count++;
                FileHelper.AppendLine(_logFilePath, $"Consumed message from topic {msg.Topic} Offset {msg.Offset.Value} / {msg.Partition.Value}");
                FileHelper.WriteLine($"{actualPath}/{actualFilePrefix}{count}.json", msg.Message.Value);
            }
        });
    }

    public void ConsumeMessageFromKafka(
        string topic,
        string consumerGroup,
        int safetyTimeoutSeconds,
        DateTime offSetResetTime,
        Func<string, string, string> GetKafkaMessage,
        string actualPath,
        string actualFilePrefix = "")
    {
        int count = FileHelper.GetNextFileCount(actualPath, actualFilePrefix);

        using var consumer = BuildConsumer(consumerGroup);

        var startOffsets = GetOffsets(topic, consumerGroup, offSetResetTime, consumer);
        LogAndAssignOffsets(topic, startOffsets, consumer);

        ConsumeUntilEnd(consumer, startOffsets, safetyTimeoutSeconds, msg =>
        {
            if (!string.IsNullOrEmpty(actualPath))
            {
                count++;
                FileHelper.AppendLine(_logFilePath, $"Consumed message from topic {msg.Topic} Offset {msg.Offset.Value} / {msg.Partition.Value}");
                var deserializedMessage = GetKafkaMessage(msg.Message.Value, msg.Topic);
                if (deserializedMessage != null)
                    FileHelper.WriteLine($"{actualPath}/{actualFilePrefix}{count}.json", deserializedMessage);
            }
        });
    }

    private IConsumer<string, string> BuildConsumer(string consumerGroup)
    {
        var config = !string.IsNullOrEmpty(_saslOauthbearerConfig)
            ? KafkaConfig.GetConsumerConfig(_broker, _saslOauthbearerConfig, consumerGroup)
            : KafkaConfig.GetConsumerConfig(_broker, _username, _password, consumerGroup);

        var builder = new ConsumerBuilder<string, string>(config)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8);

        if (!string.IsNullOrEmpty(_saslOauthbearerConfig))
            builder = builder.SetOAuthBearerTokenRefreshHandler(KafkaOauthHandler.OAuthBearerTokenRefreshHandler);

        return builder.Build();
    }

    private List<TopicPartitionOffset> GetOffsets(string topic, string consumerGroup, DateTime offSetResetTime, IConsumer<string, string> consumer)
    {
        return !string.IsNullOrEmpty(_saslOauthbearerConfig)
            ? KafkaExtensions.GetTopicOffsets(_broker, _saslOauthbearerConfig, topic, consumerGroup, offSetResetTime, consumer)
            : KafkaExtensions.GetTopicOffsets(_broker, _username, _password, topic, consumerGroup, offSetResetTime, consumer);
    }

    private void LogAndAssignOffsets(string topic, List<TopicPartitionOffset> offSets, IConsumer<string, string> consumer)
    {
        FileHelper.AppendLine(_logFilePath, $"Offsets for topic {topic} are: {string.Join(", ", offSets.Select(o => $" {Environment.NewLine} Partition: {o.Partition.Value} {Environment.NewLine} Offset: {o.Offset.Value}"))}");
        consumer.Assign(offSets);
        FileHelper.AppendLine(_logFilePath, "Consuming messages from topic: " + topic);
    }

    /// <summary>
    /// Consumes all messages from each partition up to the high watermark snapshotted at the
    /// start of the call. <paramref name="safetyTimeoutSeconds"/> acts as a ceiling to avoid
    /// hanging indefinitely if a broker error or unexpected gap occurs.
    /// </summary>
    private void ConsumeUntilEnd(
        IConsumer<string, string> consumer,
        List<TopicPartitionOffset> startOffsets,
        int safetyTimeoutSeconds,
        Action<ConsumeResult<string, string>> onMessage)
    {        
        // Snapshot high watermarks per partition with contextual error handling.
        // High = next offset to be written, so the last available message sits at High - 1.
        // Only include partitions with a valid start offset (>= 0).
        var highWatermarks = new Dictionary<TopicPartition, long>();
        foreach (var tpo in startOffsets.Where(tpo => tpo.Offset.Value >= 0))
        {
            try
            {
                var watermark = consumer.QueryWatermarkOffsets(tpo.TopicPartition, TimeSpan.FromSeconds(10));
                highWatermarks[tpo.TopicPartition] = watermark.High.Value;
            }
            catch (KafkaException e)
            {
                FileHelper.AppendLine(_logFilePath,
                    $"Failed to query watermark offsets for topic '{tpo.Topic}' partition {tpo.Partition.Value}: " +
                    $"{e.Message}, Reason: {e.Error.Reason}");
                throw;
            }
            catch (Exception e)
            {
                FileHelper.AppendLine(_logFilePath,
                    $"Failed to query watermark offsets for topic '{tpo.Topic}' partition {tpo.Partition.Value}: {e.Message}");
                throw;
            }
        }

        // Track only partitions that actually have messages between startOffset and high watermark.
        var pendingPartitions = highWatermarks
            .Where(kv =>
            {
                var startOffset = startOffsets.First(s => s.TopicPartition == kv.Key).Offset.Value;
                return kv.Value > startOffset;
            })
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        FileHelper.AppendLine(_logFilePath,
            $"Partitions to consume: {pendingPartitions.Count}. " +
            $"High watermarks: {string.Join(", ", pendingPartitions.Select(kv => $"Partition {kv.Key.Partition.Value} → offset {kv.Value - 1}"))}");

        if (pendingPartitions.Count == 0)
        {
            FileHelper.AppendLine(_logFilePath, "No new messages available in any partition.");
            consumer.Close();
            return;
        }

        var watch = Stopwatch.StartNew();
        var safetyTimeout = TimeSpan.FromSeconds(safetyTimeoutSeconds);

        while (pendingPartitions.Count > 0 && watch.Elapsed < safetyTimeout)
        {
            try
            {
                var remaining = safetyTimeout - watch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                    break;

                var msg = consumer.Consume(remaining);

                if (msg?.Message?.Value is null)
                    continue;

                onMessage(msg);

                // Once a partition's last known message is consumed, stop tracking it.
                if (pendingPartitions.TryGetValue(msg.TopicPartition, out var highWatermark)
                    && msg.Offset.Value >= highWatermark - 1)
                {
                    pendingPartitions.Remove(msg.TopicPartition);
                    FileHelper.AppendLine(_logFilePath,
                        $"Partition {msg.TopicPartition.Partition.Value} fully consumed at offset {msg.Offset.Value}.");
                }
            }
            catch (KafkaException e)
            {
                FileHelper.AppendLine(_logFilePath, $"Error: {e.Message}, Reason: {e.Error.Reason}");
                throw;
            }
            catch (Exception e)
            {
                FileHelper.AppendLine(_logFilePath, $"Error: {e.Message}");
                throw;
            }
        }

        if (pendingPartitions.Count > 0)
            FileHelper.AppendLine(_logFilePath,
                $"Safety timeout reached. Partitions not fully consumed: " +
                $"{string.Join(", ", pendingPartitions.Select(kv => $"Partition {kv.Key.Partition.Value} (last offset: {kv.Value - 1})"))}");
        else
            FileHelper.AppendLine(_logFilePath, "All partitions fully consumed.");

        consumer.Close();
    }
}
