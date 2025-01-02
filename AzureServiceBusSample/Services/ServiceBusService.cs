using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureServiceBusSample.Options;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace AzureServiceBusSample.Services
{
    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _serviceBusAdministrationClient;
        private readonly ILogger<ServiceBusService> _logger;
        public ServiceBusService(IOptions<AzureOptions> options, ILogger<ServiceBusService> logger)
        {
            if (options.Value.ConnectionString is null)
                throw new Exception("Azure Options Connection string is null");

            var connectionString = options.Value.ConnectionString;

            _serviceBusAdministrationClient = new ServiceBusAdministrationClient(connectionString);
            _serviceBusClient = new ServiceBusClient(connectionString);
            _logger = logger;
        }

        private async Task<bool> QueueExistsAsync(string name)
        {

            return await UseTryCatch(async () =>
                        {
                            return await _serviceBusAdministrationClient.QueueExistsAsync(name);
                        });



            //try
            //{
            //    return await _serviceBusAdministrationClient.QueueExistsAsync(name);
            //}
            //catch (Exception)
            //{

            //    return false;
            //}
        }

        private async Task<bool> TopicExistsAsync(string name)
        {


            return await UseTryCatch(async () =>
              {
                  return await _serviceBusAdministrationClient.TopicExistsAsync(name);
              });



            //try
            //{
            //    return await _serviceBusAdministrationClient.TopicExistsAsync(name);
            //}
            //catch (Exception)
            //{

            //    return false;
            //}


        }

        private async Task<bool> SubscriptionExistsAsync(string topicName, string subscriptionName)
        {

            return await UseTryCatch(async () =>
              {

                  return await _serviceBusAdministrationClient.SubscriptionExistsAsync(topicName, subscriptionName);
              });



            //try
            //{
            //    return await _serviceBusAdministrationClient.SubscriptionExistsAsync(topicName, subscriptionName);
            //}
            //catch (Exception)
            //{
            //    return false;
            //}
        }

        private ServiceBusReceiver GetReceiver(string queueOrTopicName, [Optional] string subscriptionName)
        {
            return subscriptionName is null
                ? _serviceBusClient.CreateReceiver(queueOrTopicName)
                : _serviceBusClient.CreateReceiver(queueOrTopicName, subscriptionName);
        }

        private ServiceBusSender GetSender(string queueOrTopicName)
        {
            return _serviceBusClient.CreateSender(queueOrTopicName);
        }

        public async Task<bool> CreateQueueAsync(string name, TimeSpan lockDuration, int maxDeliveryCount, long maxQueueSizeMB)
        {
            if (await QueueExistsAsync(name))
                return false;

            var queueOptions = new CreateQueueOptions(name)
            {
                LockDuration = lockDuration,
                MaxDeliveryCount = maxDeliveryCount,
                MaxSizeInMegabytes = maxQueueSizeMB
            };

            await _serviceBusAdministrationClient.CreateQueueAsync(queueOptions);
            return true;
        }

        public async Task<bool> CreateSubscriptionAsync(string topicName, string subscriptionName, [Optional] string ruleName, [Optional] string filter)
        {
            if (!await TopicExistsAsync(topicName))
                throw new Exception("Topic does not exist");

            if (await SubscriptionExistsAsync(topicName, subscriptionName))
                return false;

            var options = new CreateSubscriptionOptions(topicName, subscriptionName);

            await _serviceBusAdministrationClient.CreateSubscriptionAsync(options);

            if (!string.IsNullOrEmpty(ruleName) && !string.IsNullOrEmpty(filter))
            {
                var ruleOptions = new CreateRuleOptions(ruleName, new SqlRuleFilter(filter));
                await _serviceBusAdministrationClient.CreateRuleAsync(topicName, subscriptionName, ruleOptions);
            }

            return true;
        }

        public async Task<bool> CreateTopicAsync(string name, long maxTopicSizeMB)
        {
            if (await TopicExistsAsync(name))
                return false;

            var topicOptions = new CreateTopicOptions(name)
            {
                MaxSizeInMegabytes = maxTopicSizeMB
            };

            await _serviceBusAdministrationClient.CreateTopicAsync(topicOptions);
            return true;
        }

        public async Task<bool> DeleteQueueAsync(string name)
        {
            if (!await QueueExistsAsync(name))
                return false;


            return await UseTryCatch(async () =>
            {
                await _serviceBusAdministrationClient.DeleteQueueAsync(name);
                return true;
            });


            //try
            //{
            //    await _serviceBusAdministrationClient.DeleteQueueAsync(name);
            //}
            //catch (Exception)
            //{

            //    return false;
            //}

            //return true;
        }

        public async Task<bool> DeleteSubscriptionAsync(string topicName, string subscriptionName)
        {
            if (!await SubscriptionExistsAsync(topicName, subscriptionName))
                return false;


            return await UseTryCatch(async () =>
            {
                await _serviceBusAdministrationClient.DeleteSubscriptionAsync(topicName, subscriptionName);
                return true;
            });

            //try
            //{
            //    await _serviceBusAdministrationClient.DeleteSubscriptionAsync(topicName, subscriptionName);
            //}
            //catch (Exception)
            //{

            //    return false;
            //}

            //return true;
        }

        public async Task<bool> DeleteTopicAsync(string name)
        {
            if (!await TopicExistsAsync(name))
                return false;

            return await UseTryCatch(async () =>
            {

                await _serviceBusAdministrationClient.DeleteTopicAsync(name);
                return true;
            });


            //try
            //{
            //    await _serviceBusAdministrationClient.DeleteTopicAsync(name);
            //}
            //catch (Exception)
            //{

            //    return false;
            //}
            //return true;
        }

        public async Task<IEnumerable<string>> ListQueueNamesAsync()
        {
            var queues = _serviceBusAdministrationClient.GetQueuesAsync();
            var queueNames = new List<string>();

            await foreach (var queue in queues)
            {
                queueNames.Add(queue.Name);
            }

            return queueNames;
        }

        public async Task<IEnumerable<string>> ListTopicNamesAsync()
        {
            var topics = _serviceBusAdministrationClient.GetTopicsAsync();
            var topicNames = new List<string>();

            await foreach (var topic in topics)
            {
                topicNames.Add(topic.Name);
            }

            return topicNames;
        }

        public async Task<IEnumerable<(bool, BinaryData?)>> ReceiveQueueAsync(string queueName, int messageCount)
        {

            var receiver = GetReceiver(queueName);

            var messages = await receiver.ReceiveMessagesAsync(messageCount);

            return await CompleteAndGetMessagesBodyAsync(receiver, messages);
        }

        public async Task<IEnumerable<(bool, BinaryData?)>> ReceiveSubscriptionAsync(string topicName, string subscriptionName, int messageCount)
        {
            var receiver = GetReceiver(topicName, subscriptionName);
            var messages = await receiver.ReceiveMessagesAsync(messageCount);


            return await CompleteAndGetMessagesBodyAsync(receiver, messages); ;
        }

        public async Task<bool> SendMessageToQueueAsync(string queueName, BinaryData message, [Optional] DateTimeOffset scheduleAt)
        {
            var sender = GetSender(queueName);
            var serviceBusMessage = new ServiceBusMessage(message);


            return await SendMessageAsync(sender, serviceBusMessage, scheduleAt);

        }

        public async Task<bool> SendMessageToTopicAsync(string topicName, BinaryData message, [Optional] DateTimeOffset scheduleAt)
        {
            var sender = GetSender(topicName);
            var serviceBusMessage = new ServiceBusMessage(message);
            return await SendMessageAsync(sender, serviceBusMessage, scheduleAt);
        }

        private async Task<bool> SendMessageAsync(ServiceBusSender sender, ServiceBusMessage message, [Optional] DateTimeOffset scheduleAt)
        {

            return await UseTryCatch(async () =>
            {
                if (scheduleAt == default)
                    await sender.SendMessageAsync(message);
                else
                    await sender.ScheduleMessageAsync(message, scheduleAt);

                return true;

            });

            //try
            //{
            //    if (scheduleAt == default)
            //        await sender.SendMessageAsync(message);
            //    else
            //        await sender.ScheduleMessageAsync(message, scheduleAt);

            //    return true;
            //}
            //catch
            //{

            //    return false;
            //}


        }

        private async Task<IEnumerable<(bool, BinaryData?)>> CompleteAndGetMessagesBodyAsync(ServiceBusReceiver receiver, IReadOnlyList<ServiceBusReceivedMessage> messages)
        {
            var returnList = new List<(bool, BinaryData?)>(capacity: messages.Count);

            foreach (var message in messages)
            {

                try
                {
                    await receiver.CompleteMessageAsync(message);
                    returnList.Add((true, message.Body));
                }
                catch (Exception ex)
                {
                    returnList.Add((false, null));
                }

            }
            return returnList;

        }

        private async Task<bool> UseTryCatch(Func<Task<bool>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                return false;
            }

        }


    }
}
