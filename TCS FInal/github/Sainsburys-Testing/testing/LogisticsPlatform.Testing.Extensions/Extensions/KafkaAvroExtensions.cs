using Confluent.Kafka;
using LogisticsPlatform.Common.Extensions.Kafka;

namespace LogisticsPlatform.Testing.Extensions.Extensions;

public class KafkaAvroExtensions
{   
    public static (List<TopicPartitionOffset> Offsets, IConsumer<string, string> BootstrapConsumer) GetTopicOffsets(
            string broker,
            string saslOauthbearerConfig,
            string topic,
            string consumerGroup,
            DateTime offSetResetTime)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = broker,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SaslOauthbearerConfig = saslOauthbearerConfig
        };

        var topicPartitions = GetTopicPartitions(adminConfig, topic, saslOauthbearerConfig);

        var bootstrapConfig = KafkaConfig.GetConsumerConfig(broker, saslOauthbearerConfig, consumerGroup);

        // Bootstrap consumer is intentionally NOT disposed here — it is returned
        // to the caller so it can be reused for QueryWatermarkOffsets to confirm
        // the Avro consumer's partitions are reachable before consuming starts.
        var bootstrapConsumer = new ConsumerBuilder<string, string>(bootstrapConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetOAuthBearerTokenRefreshHandler(KafkaOauthHandler.OAuthBearerTokenRefreshHandler)
            .Build();

        var offsets = GetOffsetsForTimes(bootstrapConsumer, topicPartitions, offSetResetTime);
        return (offsets, bootstrapConsumer);
    }
    
    public static (List<TopicPartitionOffset> Offsets, IConsumer<string, string> BootstrapConsumer) GetTopicOffsets(
        string broker,
        string username,
        string password,
        string topic,
        string consumerGroup,
        DateTime offSetResetTime)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = broker,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = username,
            SaslPassword = password
        };

        var topicPartitions = GetTopicPartitions(adminConfig, topic);

        var bootstrapConfig = KafkaConfig.GetConsumerConfig(broker, username, password, consumerGroup);

        // Bootstrap consumer is intentionally NOT disposed here — it is returned
        // to the caller so it can be reused for QueryWatermarkOffsets to confirm
        // the Avro consumer's partitions are reachable before consuming starts.
        var bootstrapConsumer = new ConsumerBuilder<string, string>(bootstrapConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        var offsets = GetOffsetsForTimes(bootstrapConsumer, topicPartitions, offSetResetTime);
        return (offsets, bootstrapConsumer);
    }
    
    private static List<TopicPartition> GetTopicPartitions(
        AdminClientConfig adminConfig,
        string topic,
        string saslOauthbearerConfig = null)
        => KafkaExtensions.GetTopicPartitions(adminConfig, topic, saslOauthbearerConfig);

    private static List<TopicPartitionOffset> GetOffsetsForTimes(
        IConsumer<string, string> bootstrapConsumer,
        List<TopicPartition> topicPartitions,
        DateTime offSetResetTime)
    {
        bootstrapConsumer.Assign(topicPartitions);
        KafkaExtensions.WaitForBrokerConnection(bootstrapConsumer, topicPartitions);

        var newTimes = topicPartitions
            .Select(tp => new TopicPartitionTimestamp(tp, new Timestamp(offSetResetTime)))
            .ToList();

        return bootstrapConsumer.OffsetsForTimes(newTimes, TimeSpan.FromSeconds(30));
    }
}
