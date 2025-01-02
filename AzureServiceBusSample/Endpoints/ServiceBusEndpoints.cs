using AzureServiceBusSample.Options;
using AzureServiceBusSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureServiceBusSample.Endpoints;

public static class ServiceBusEndpoints
{

    public static WebApplication AddAzureServiceBusEndpoints(this WebApplication? app)
    {
        if (app is null)
            throw new ArgumentNullException("WebApplication is null !");



        app.MapPost("/queues/create", async (IServiceBusService service, string name, TimeSpan lockDuration, int maxDeliveryCount, long maxQueueSizeMB) =>
        {
            var result = await service.CreateQueueAsync(name, lockDuration, maxDeliveryCount, maxQueueSizeMB);
            return result ? Results.Ok() : Results.BadRequest("Queue creation failed.");
        });


        app.MapPost("/topics/create", async (IServiceBusService service, string name, long maxTopicSizeMB) =>
        {
            var result = await service.CreateTopicAsync(name, maxTopicSizeMB);
            return result ? Results.Ok() : Results.BadRequest("Topic creation failed.");
        });

        app.MapPost("/subscriptions/create", async (IServiceBusService service, string topicName, string subscriptionName, string? ruleName = null, string? filter = null) =>
        {
            var result = await service.CreateSubscriptionAsync(topicName, subscriptionName, ruleName, filter);
            return result ? Results.Ok() : Results.BadRequest("Subscription creation failed.");
        });

        app.MapDelete("/queues/{name}", async (IServiceBusService service, [FromRoute] string name) =>
        {
            var result = await service.DeleteQueueAsync(name);
            return result ? Results.Ok() : Results.NotFound("Queue not found.");
        });

        app.MapDelete("/topics/{name}", async (IServiceBusService service, [FromRoute]string name) =>
        {
            var result = await service.DeleteTopicAsync(name);
            return result ? Results.Ok() : Results.NotFound("Topic not found.");
        });

        app.MapDelete("/subscriptions/{topicName}/{subscriptionName}", async (IServiceBusService service, [FromRoute]string topicName, [FromRoute]string subscriptionName) =>
        {
            var result = await service.DeleteSubscriptionAsync(topicName, subscriptionName);
            return result ? Results.Ok() : Results.NotFound("Subscription not found.");
        });

        app.MapGet("/queues", async (IServiceBusService service) =>
        {
            var queues = await service.ListQueueNamesAsync();
            return Results.Ok(queues);
        });

        app.MapGet("/topics", async (IServiceBusService service) =>
        {
            var topics = await service.ListTopicNamesAsync();
            return Results.Ok(topics);
        });

        app.MapPost("/queues/{queueName}/send", async (IServiceBusService service, string queueName, [FromBody]string message, DateTimeOffset? scheduleAt = null) =>
        {
            var result = await service.SendMessageToQueueAsync(queueName, new BinaryData(message), scheduleAt ?? DateTimeOffset.MinValue);
            return result ? Results.Ok() : Results.BadRequest("Failed to send message to queue.");
        });

        app.MapPost("/topics/{topicName}/send", async (IServiceBusService service, string topicName, [FromBody]string message, DateTimeOffset? scheduleAt = null) =>
        {
            var result = await service.SendMessageToTopicAsync(topicName, new BinaryData(message), scheduleAt ?? DateTimeOffset.MinValue);
            return result ? Results.Ok() : Results.BadRequest("Failed to send message to topic.");
        });

        app.MapGet("/queues/{queueName}/receive", async (IServiceBusService service, string queueName, int messageCount) =>
        {
            var messages = await service.ReceiveQueueAsync(queueName, messageCount);
            return Results.Ok(messages);
        });

        app.MapGet("/topics/{topicName}/subscriptions/{subscriptionName}/receive", async (IServiceBusService service, string topicName, string subscriptionName, int messageCount) =>
        {
            var messages = await service.ReceiveSubscriptionAsync(topicName, subscriptionName, messageCount);
            return Results.Ok(messages);
        });

        return app;
    }


}
