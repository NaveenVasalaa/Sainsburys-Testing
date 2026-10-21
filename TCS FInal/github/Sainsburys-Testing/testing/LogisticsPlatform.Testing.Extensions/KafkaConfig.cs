using Confluent.Kafka;

namespace LogisticsPlatform.Testing.Extensions
{
    public class KafkaConfig
    {
        public static ConsumerConfig GetConsumerConfig(string broker, string username, string password, string consumerGroup)
            => CreateConsumerConfig(broker, consumerGroup, SaslMechanism.Plain, username, password, null);

        public static ConsumerConfig GetConsumerConfig(string broker, string saslOauthbearerConfig, string consumerGroup)
            => CreateConsumerConfig(broker, consumerGroup, SaslMechanism.OAuthBearer, null, null, saslOauthbearerConfig);

        public static ProducerConfig GetProducerConfig(string broker, string username, string password)
            => CreateProducerConfig(broker, SaslMechanism.Plain, username, password, null);

        public static ProducerConfig GetProducerConfig(string broker, string saslOauthbearerConfig)
            => CreateProducerConfig(broker, SaslMechanism.OAuthBearer, null, null, saslOauthbearerConfig);

        private static ConsumerConfig CreateConsumerConfig(
            string broker, string groupId, SaslMechanism mechanism,
            string username, string password, string oauthConfig)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = broker,
                GroupId = groupId,
                EnableAutoCommit = true,
                SecurityProtocol = SecurityProtocol.SaslSsl,                
                SaslMechanism = mechanism
            };
            if (mechanism == SaslMechanism.Plain)
            {
                config.SaslUsername = username;
                config.SaslPassword = password;
            }
            else if (mechanism == SaslMechanism.OAuthBearer)
            {
                config.SaslOauthbearerConfig = oauthConfig;
            }
            return config;
        }

        private static ProducerConfig CreateProducerConfig(
            string broker, SaslMechanism mechanism,
            string username, string password, string oauthConfig)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = broker,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = mechanism,
                CompressionType = CompressionType.Gzip,
                CompressionLevel = 5,
                MessageTimeoutMs = 30000
            };
            if (mechanism == SaslMechanism.Plain)
            {
                config.SaslUsername = username;
                config.SaslPassword = password;
            }
            else if (mechanism == SaslMechanism.OAuthBearer)
            {
                config.SaslOauthbearerConfig = oauthConfig;
            }
            return config;
        }
    }
}
