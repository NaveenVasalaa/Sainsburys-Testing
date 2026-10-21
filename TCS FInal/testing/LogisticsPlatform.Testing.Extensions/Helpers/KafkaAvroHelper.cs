using Chr.Avro.Confluent;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using LogisticsPlatform.Common.Extensions;
using LogisticsPlatform.Common.Extensions.Kafka;
using LogisticsPlatform.Common.Extensions.Kafka.Enums;
using LogisticsPlatform.Common.Extensions.Kafka.Settings;
using LogisticsPlatform.Testing.Extensions.Extensions;
using System.Diagnostics;
using System.Text.Json;
using KafkaExtensions = LogisticsPlatform.Testing.Extensions.Extensions.KafkaExtensions;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class KafkaAvroHelper<T> where T : class
{
    private readonly string _broker;
    private readonly string _username;
    private readonly string _password;
    private readonly string _saslOauthbearerConfig;
    private readonly string _schemaRegistryUrl;
    private readonly string _schemaRegistryAuth;
    private readonly string _schemaRegistryOauthBearerConfig;
    private readonly string _logFilePath;
    private readonly KafkaSettingsBase _producerSettings;
    private CachedSchemaRegistryClient _registry;
    private readonly KafkaAvroService<T> _kafkaAvroService = new();

    /// <summary>
    /// Constructor for plain SASL (username/password) authentication.
    /// schemaRegistryAuth = Basic Auth string "username:password"
    /// </summary>
    public KafkaAvroHelper(string broker, string username, string password,
        string schemaRegistryUrl, string schemaRegistryAuth, string logFilePath)
    {
        _broker = broker;
        _username = username;
        _password = password;
        _schemaRegistryUrl = schemaRegistryUrl;
        _schemaRegistryAuth = schemaRegistryAuth;
        _logFilePath = logFilePath;

        _producerSettings = new KafkaSettingsBase
        {
            Brokers = broker,
            UserName = username,
            Password = password,
            SchemaRegistryUrl = schemaRegistryUrl,
            SchemaRegistryAuth = schemaRegistryAuth
        };
    }

    /// <summary>
    /// Constructor for OAuthBearer authentication.
    /// saslOauthbearerConfig           = Kafka broker OAuth config  "logicalCluster=lkc-xxxxx&scope=...&identityPool=..."
    /// schemaRegistryOauthBearerConfig = Schema Registry OAuth config "logicalCluster=lsrc-xxxxx&scope=...&identityPool=..."
    /// These MUST be different values — lkc-xxxxx for Kafka, lsrc-xxxxx for Schema Registry.
    /// </summary>
    public KafkaAvroHelper(string broker, string saslOauthbearerConfig,
        string schemaRegistryUrl, string avroSchemaRegistryOauthBearerConfig, string logFilePath)
    {
        _broker = broker;
        _saslOauthbearerConfig = saslOauthbearerConfig;
        _schemaRegistryUrl = schemaRegistryUrl;
        _schemaRegistryOauthBearerConfig = avroSchemaRegistryOauthBearerConfig;
        _logFilePath = logFilePath;

        _producerSettings = new KafkaSettingsBase
        {
            Brokers = broker,
            SaslOauthBearerConfig = saslOauthbearerConfig,
            SchemaRegistryUrl = schemaRegistryUrl,
            SchemaRegistryOauthBearerConfig = avroSchemaRegistryOauthBearerConfig,
            KafkaAuthType = KafkaAuthType.OauthConfig
        };
    }

    public async Task SendMessageToKafkaAsync(string topic, string key, string inputPath, IEnumerable<IHeader> headers = null)
    {
        _producerSettings.TopicName = topic;

        var inputFiles = FileHelper.GetAllFiles(inputPath);
        foreach (var inputFile in inputFiles)
        {
            var content = FileHelper.GetAllText(inputFile);
            var value = JsonSerializer.Deserialize<T>(content);
            var result = await _kafkaAvroService.PublishToKafkaAsync(_producerSettings, topic, key, value, customHeaders: headers).ConfigureAwait(false);
            LogProduceResult(topic, result.Response, result.Partition ?? -1, result.Offset ?? -1, result.ExceptionMessage);
        }
    }

    public async Task SendMessageToKafkaAsync(string topic, string key, T value, IEnumerable<IHeader> headers = null)
    {
        _producerSettings.TopicName = topic;
        var result = await _kafkaAvroService.PublishToKafkaAsync(_producerSettings, topic, key, value, customHeaders: headers).ConfigureAwait(false);
        LogProduceResult(topic, result.Response, result.Partition ?? -1, result.Offset ?? -1, result.ExceptionMessage);
    }

    private void LogProduceResult(string topic, bool success, int partition, long offset, string errorMessage)
    {
        if (!success)
            FileHelper.AppendLine(_logFilePath, $"Error while publishing message to Kafka topic '{topic}': {errorMessage}");
        else
            FileHelper.AppendLine(_logFilePath, $"Message published to {topic} (Partition: {partition}, Offset: {offset})");
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

        var startOffsets = GetOffsets(topic, consumerGroup, offSetResetTime, out var bootstrapConsumer);
        LogAndAssignOffsets(topic, startOffsets, consumer, bootstrapConsumer);

        ConsumeUntilEnd(consumer, startOffsets, safetyTimeoutSeconds, msg =>
        {
            if (!string.IsNullOrEmpty(actualPath))
            {
                count++;
                FileHelper.AppendLine(_logFilePath, $"Consumed message from topic {msg.Topic} Offset {msg.Offset.Value} / {msg.Partition.Value}");
                FileHelper.WriteLine($"{actualPath}/{actualFilePrefix}{count}.json",
                    msg.Message.Value.ToJsonString(JsonSerializerExtensions.DefaultSerializerOptions));
            }
        });
    }

    public void ConsumeMessageFromKafka(
        string topic,
        string consumerGroup,
        int safetyTimeoutSeconds,
        DateTime offSetResetTime,
        Func<string, T, string> GetKafkaMessage,
        string actualPath,
        string actualFilePrefix = "")
    {
        int count = FileHelper.GetNextFileCount(actualPath, actualFilePrefix);

        using var consumer = BuildConsumer(consumerGroup);

        var startOffsets = GetOffsets(topic, consumerGroup, offSetResetTime, out var bootstrapConsumer);
        LogAndAssignOffsets(topic, startOffsets, consumer, bootstrapConsumer);

        ConsumeUntilEnd(consumer, startOffsets, safetyTimeoutSeconds, msg =>
        {
            if (!string.IsNullOrEmpty(actualPath))
            {
                count++;
                FileHelper.AppendLine(_logFilePath, $"Consumed message from topic {msg.Topic} Offset {msg.Offset.Value} / {msg.Partition.Value}");
                var deserializedMessage = GetKafkaMessage(msg.Topic, msg.Message.Value);
                if (deserializedMessage != null)
                    FileHelper.WriteLine($"{actualPath}/{actualFilePrefix}{count}.json", deserializedMessage);
            }
        });
    }

    private IConsumer<string, T> BuildConsumer(string consumerGroup)
    {
        var config = !string.IsNullOrEmpty(_saslOauthbearerConfig)
            ? KafkaConfig.GetConsumerConfig(_broker, _saslOauthbearerConfig, consumerGroup)
            : KafkaConfig.GetConsumerConfig(_broker, _username, _password, consumerGroup);

        SetSchemaRegistryClient();

        var builder = new ConsumerBuilder<string, T>(config)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetAvroValueDeserializer(_registry);

        // Required for OAuthBearer — without this the broker SASL handshake
        // never completes and Consume() returns null silently.
        if (!string.IsNullOrEmpty(_saslOauthbearerConfig))
            builder = builder.SetOAuthBearerTokenRefreshHandler(KafkaOauthHandler.OAuthBearerTokenRefreshHandler);

        return builder.Build();
    }

    private List<TopicPartitionOffset> GetOffsets(string topic, string consumerGroup, DateTime offSetResetTime,
        out IConsumer<string, string> bootstrapConsumer)
    {
        var result = !string.IsNullOrEmpty(_saslOauthbearerConfig)
            ? KafkaAvroExtensions.GetTopicOffsets(_broker, _saslOauthbearerConfig, topic, consumerGroup, offSetResetTime)
            : KafkaAvroExtensions.GetTopicOffsets(_broker, _username, _password, topic, consumerGroup, offSetResetTime);

        // Use named tuple properties — consistent with KafkaAvroExtensions return type
        bootstrapConsumer = result.BootstrapConsumer;
        return result.Offsets;
    }

    private void LogAndAssignOffsets(string topic, List<TopicPartitionOffset> offSets,
        IConsumer<string, T> consumer, IConsumer<string, string> bootstrapConsumer)
    {
        foreach (var o in offSets)
        {
            FileHelper.AppendLine(_logFilePath,
                $"Offset for topic {topic} — Partition: {o.Partition.Value}, Offset: {o.Offset.Value}");
        }

        consumer.Assign(offSets);

        try
        {
            KafkaExtensions.VerifyPartitionsReachableViaBootstrap(bootstrapConsumer, offSets);
        }
        finally
        {
            bootstrapConsumer.Close();
            bootstrapConsumer.Dispose();
        }

        FileHelper.AppendLine(_logFilePath, "Consuming messages from topic: " + topic);
    }

    /// <summary>
    /// Consumes all messages from each partition up to the high watermark snapshotted at the
    /// start of the call. <paramref name="safetyTimeoutSeconds"/> acts as a ceiling to avoid
    /// hanging indefinitely if a broker error or unexpected gap occurs.
    /// <para>
    /// <see cref="IConsumer{TKey,TValue}.QueryWatermarkOffsets"/> is a pure broker metadata
    /// call — it does not trigger Avro deserialization and is safe to use on this consumer.
    /// For OAuthBearer, we allow the connection to stabilize before querying offsets.
    /// </para>
    /// </summary>
    private void ConsumeUntilEnd(
        IConsumer<string, T> consumer,
        List<TopicPartitionOffset> startOffsets,
        int safetyTimeoutSeconds,
        Action<ConsumeResult<string, T>> onMessage)
    {
        // For OAuthBearer, allow the connection to stabilize before querying watermarks.
        // This ensures the SASL handshake and token refresh completes.
        if (!string.IsNullOrEmpty(_saslOauthbearerConfig))
            KafkaOauthHandler.OAuthBearerTokenRefreshHandler(consumer, _saslOauthbearerConfig);            

        // Snapshot high watermarks per partition with contextual error handling.
        // High = next offset to be written, so the last available message sits at High - 1.
        // QueryWatermarkOffsets is a pure metadata call — Avro deserialization is never triggered.
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

                // Remove partition from pending once its last known message is consumed.
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

    private void SetSchemaRegistryClient()
    {
        if (_registry != null)
            return;

        var schemaRegistryConfig = new SchemaRegistryConfig { Url = _schemaRegistryUrl };

        if (!string.IsNullOrEmpty(_schemaRegistryOauthBearerConfig))
        {
            schemaRegistryConfig.BearerAuthCredentialsSource = BearerAuthCredentialsSource.Custom;
            _registry = new CachedSchemaRegistryClient(
                schemaRegistryConfig,
                new KafkaOAuthBearerTokenHeaderProvider(_schemaRegistryOauthBearerConfig));
        }
        else if (!string.IsNullOrEmpty(_schemaRegistryAuth))
        {
            schemaRegistryConfig.BasicAuthCredentialsSource = AuthCredentialsSource.UserInfo;
            schemaRegistryConfig.BasicAuthUserInfo = _schemaRegistryAuth;
            _registry = new CachedSchemaRegistryClient(schemaRegistryConfig);
        }
        else
        {
            _registry = new CachedSchemaRegistryClient(schemaRegistryConfig);
        }
    }
}
