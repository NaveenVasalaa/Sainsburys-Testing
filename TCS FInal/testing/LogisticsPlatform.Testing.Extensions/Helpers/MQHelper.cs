using IBM.WMQ;
using LogisticsPlatform.Testing.Extensions.Models;
using System.Collections;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class MQHelper
{
    private readonly MQOptions _mQOptions;
    private readonly Hashtable _connectionProperties;
    private readonly string _logFilePath;

    public MQHelper(MQOptions mQOptions, string logFilePath)
    {
        _mQOptions = mQOptions;
        _connectionProperties = new Hashtable
        {
            { MQC.HOST_NAME_PROPERTY, _mQOptions.HostName },
            { MQC.PORT_PROPERTY, _mQOptions.PortNumber },
            { MQC.CHANNEL_PROPERTY, _mQOptions.ChannelId },
            { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
            { MQC.CONNECT_OPTIONS_PROPERTY, MQC.MQCNO_RECONNECT }
        };
        _logFilePath = logFilePath;
    }

    public static MQMessage GetMqMessage(string message)
    {
        MQMessage queueMessage = new MQMessage();
        queueMessage.Write(Encoding.UTF8.GetBytes(message));
        queueMessage.Format = MQC.MQFMT_STRING;
        queueMessage.Persistence = MQC.MQPER_PERSISTENT;
        queueMessage.CharacterSet = 1208;
        return queueMessage;
    }

    public void PostMessagesToMQ(string inputPath, string inputFilePrefix = null)
    {
        try
        {
            var inputFiles = FileHelper.GetAllFiles(inputPath, inputFilePrefix + "*" );
            MQQueueManager mQQueueManager = new MQQueueManager(_mQOptions.QueueManager, _connectionProperties);
            FileHelper.AppendLine(_logFilePath, "MQ Instance has been created");
            MQQueue queue = null;

            MQPutMessageOptions queuePutMessageOptions;
            queue = mQQueueManager.AccessQueue(_mQOptions.AccessQueue, MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);
            FileHelper.AppendLine(_logFilePath, "Access Queue");
            queuePutMessageOptions = new MQPutMessageOptions();
            queuePutMessageOptions.Options += MQC.MQPMO_SYNCPOINT;

            foreach (var inputFile in inputFiles)
            {
                string[] messages = FileHelper.GetAllLines(inputFile);

                foreach (var message in messages)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        FileHelper.AppendLine(_logFilePath, $"Posting Message with length: {message.Length}");
                        queue.Put(GetMqMessage(message), queuePutMessageOptions);
                    }
                }
            }

            mQQueueManager.Commit();
            FileHelper.AppendLine(_logFilePath, "Commit queue");
            mQQueueManager.Close();
        }
        catch (MQException ex)
        {
            FileHelper.AppendLine(_logFilePath, $"Error in MQException GetQueueManager {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            FileHelper.AppendLine(_logFilePath, $"Error in Exception GetQueueManager {ex.Message}");
            throw;
        }
    }
}
