using Confluent.Kafka;
using LogisticsPlatform.Common.Extensions.Kafka;

namespace LogisticsPlatform.Testing.Extensions.Extensions
{
    public static class KafkaExtensions
    {
        public static List<TopicPartitionOffset> GetTopicOffsets(
            string broker,
            string saslOauthbearerConfig,
            string topic,
            string consumerGroup,
            DateTime offSetResetTime,
            IConsumer<string, string> consumer)
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = broker,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.OAuthBearer,
                SaslOauthbearerConfig = saslOauthbearerConfig
            };

            var topicPartitions = GetTopicPartitions(adminConfig, topic, saslOauthbearerConfig);
            return GetOffsetsForTimes(consumer, topicPartitions, offSetResetTime);
        }

        public static List<TopicPartitionOffset> GetTopicOffsets(
            string broker,
            string username,
            string password,
            string topic,
            string consumerGroup,
            DateTime offSetResetTime,
            IConsumer<string, string> consumer)
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
            return GetOffsetsForTimes(consumer, topicPartitions, offSetResetTime);
        }
        
        internal static List<TopicPartition> GetTopicPartitions(
            AdminClientConfig adminConfig,
            string topic,
            string saslOauthbearerConfig = null)
        {
            var adminClientBuilder = new AdminClientBuilder(adminConfig);
            if (!string.IsNullOrEmpty(saslOauthbearerConfig))
                adminClientBuilder = adminClientBuilder.SetOAuthBearerTokenRefreshHandler(
                    KafkaOauthHandler.OAuthBearerTokenRefreshHandler);

            using var adminClient = adminClientBuilder.Build();
            var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));
            return meta.Topics
                .Where(t => t.Topic == topic)
                .SelectMany(t => t.Partitions.Select(p => new TopicPartition(t.Topic, p.PartitionId)))
                .ToList();
        }

        private static List<TopicPartitionOffset> GetOffsetsForTimes(
            IConsumer<string, string> consumer,
            List<TopicPartition> topicPartitions,
            DateTime offSetResetTime)
        {
            consumer.Assign(topicPartitions);
            WaitForBrokerConnection(consumer, topicPartitions);

            var newTimes = topicPartitions
                .Select(tp => new TopicPartitionTimestamp(tp, new Timestamp(offSetResetTime)))
                .ToList();

            return consumer.OffsetsForTimes(newTimes, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Warms up broker connection for plain string consumers by polling via Consume.
        /// Do NOT use for Avro consumers — Consume triggers deserialization.
        /// </summary>
        internal static void WaitForBrokerConnection<TKey, TValue>(
            IConsumer<TKey, TValue> consumer,
            List<TopicPartition> topicPartitions,
            int deadlineSeconds = 10)
        {
            var deadline = DateTime.UtcNow.AddSeconds(deadlineSeconds);
            while (DateTime.UtcNow < deadline)
            {
                consumer.Consume(TimeSpan.FromMilliseconds(500));

                var watermarks = topicPartitions
                    .Select(tp =>
                    {
                        try { return consumer.GetWatermarkOffsets(tp); }
                        catch { return null; }
                    })
                    .ToList();

                if (watermarks.All(w => w != null))
                    break;
            }
        }

        /// <summary>
        /// Uses the already-connected bootstrap consumer (from KafkaAvroExtensions)
        /// to verify that active partitions are reachable via QueryWatermarkOffsets.
        /// This avoids any warmup on the Avro consumer itself.
        /// Only checks partitions with real offsets (>= 0).
        /// </summary>
        internal static void VerifyPartitionsReachableViaBootstrap(
            IConsumer<string, string> bootstrapConsumer,
            List<TopicPartitionOffset> assignedOffsets)
        {
            var activePartitions = assignedOffsets
                .Where(tpo => tpo.Offset.Value >= 0)
                .Select(tpo => tpo.TopicPartition)
                .ToList();

            if (activePartitions.Count == 0)
                return;

            // Bootstrap consumer is already connected — QueryWatermarkOffsets
            // should succeed immediately without any retry needed.
            foreach (var tp in activePartitions)
            {
                bootstrapConsumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(10));
            }
        }
    }
}
