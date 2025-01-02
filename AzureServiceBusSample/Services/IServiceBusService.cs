using System.Runtime.InteropServices;

namespace AzureServiceBusSample.Services;

public interface IServiceBusService
{
    Task<bool> CreateQueueAsync(
        string name,
        TimeSpan lockDuration,
        int maxDeliveryCount,
        long maxQueueSizeMB);

    Task<bool> CreateTopicAsync(
        string name,
        long maxTopicSizeMB);

    Task<bool> DeleteQueueAsync(string name);
    Task<bool> DeleteTopicAsync(string name);

    Task<bool> CreateSubscriptionAsync(
        string topicName,
        string subscriptionName,
        [Optional] string ruleName,
        [Optional] string filter);

    Task<bool> DeleteSubscriptionAsync(string topicName, string subscriptionName);


    Task<IEnumerable<string>> ListQueueNamesAsync();

    Task<IEnumerable<string>> ListTopicNamesAsync();

    Task<bool> SendMessageToQueueAsync(
        string queueName,
        BinaryData message,
        [Optional] DateTimeOffset scheduleAt);
    Task<bool> SendMessageToTopicAsync(
        string topicName,
        BinaryData message,
        [Optional] DateTimeOffset scheduleAt);

    Task<IEnumerable<(bool, BinaryData?)>> ReceiveSubscriptionAsync(
        string topicName,
        string subscriptionName,
        int messageCount);

    Task<IEnumerable<(bool, BinaryData?)>> ReceiveQueueAsync(
       string queueName,
       int messageCount);






}
