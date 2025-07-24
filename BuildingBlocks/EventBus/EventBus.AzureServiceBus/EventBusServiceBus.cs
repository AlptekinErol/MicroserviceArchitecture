using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    private ITopicClient topicClient;
    private ManagementClient managementClient;
    private ILogger logger;
    public EventBusServiceBus(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
    {
        logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
        managementClient = new ManagementClient(config.EventBusConnectionString);
        topicClient = CreateTopicClient();
    }

    /// <summary>
    /// This method will create a topic client for the Azure Service Bus topic.
    /// </summary>
    /// <returns></returns>
    private ITopicClient CreateTopicClient()
    {
        if (topicClient == null || topicClient.IsClosedOrClosing)
        {
            topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);
        }

        // Ensure that topic already exists
        if (!managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
        {
            managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
        }

        return topicClient;
    }

    /// <summary>
    /// This method will take the message and publish it to the Azure Service Bus topic.
    /// </summary>
    /// <param name="event"></param>
    public override void Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name; // Example: OrderCreatedIntegrationEvent

        eventName = ProcessEventName(eventName); // Example: OrderCreated

        var eventStr = JsonConvert.SerializeObject(@event);
        var bodyArr = Encoding.UTF8.GetBytes(eventStr);

        var message = new Message(bodyArr)
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = bodyArr,
            Label = eventName,
        };

        topicClient.SendAsync(message).GetAwaiter().GetResult();
    }

    /// <summary>
    /// This method will subscribe to the given event type and register the event handler.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            var subscriptionClient = CreateSubscriptionClientIfNotExists(eventName);

            RegisterSubscriptionClientMessageHandler(subscriptionClient);
        }

        logger.LogInformation("Subscribing to event {eventName} with {EventHandler}", eventName, typeof(TH).Name);

        SubsManager.AddSubscription<T, TH>(); // Add subscription to the manager
    }

    /// <summary>
    /// This method will unsubscribe from the given event type and remove the subscription from the Azure Service Bus.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    public override void UnSubscribe<T, TH>()
    {
        var eventName = typeof(T).Name;

        try
        {
            // Subscription will be there but we don't subscribe 
            var subscribeClient = CreateSubscriptionClient(eventName);

            subscribeClient
                .RemoveRuleAsync(eventName)
                .GetAwaiter()
                .GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            logger.LogWarning("The messaging entity {EventName} could not be found.", eventName);
        }

        logger.LogInformation("Unsubscribing from event {eventName}", eventName);

        SubsManager.RemoveSubscription<T, TH>(); // Remove subscription from the manager
    }

    /// <summary>
    /// This method will process the event name by removing the prefix and suffix if they exist.
    /// </summary>
    /// <param name="subscriptionClient"></param>
    private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
    {
        subscriptionClient.RegisterMessageHandler(
            async (message, token) =>
            {
                var eventName = $"{message.Label}";
                var messageData = Encoding.UTF8.GetString(message.Body);

                // Complete the message so that it is not received again.
                if (await ProcessEvent(ProcessEventName(eventName), messageData))
                {
                    await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            },
            new MessageHandlerOptions(ExceptionReceiveHandler) { MaxConcurrentCalls = 10, AutoComplete = false });

    }

    /// <summary>
    /// This method will handle the exception received from the message handler.
    /// </summary>
    /// <param name="exceptionReceivedEventArgs"></param>
    /// <returns></returns>
    private Task ExceptionReceiveHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        var ex = exceptionReceivedEventArgs.Exception;
        var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

        logger.LogError(ex, "ERROR handling message:{ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

        return Task.CompletedTask;
    }

    /// <summary>
    /// This method will subscribe to the given event name and create a subscription client if it does not exist.
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    private ISubscriptionClient CreateSubscriptionClientIfNotExists(string eventName)
    {
        var subClient = CreateSubscriptionClient(eventName);

        var exists = managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

        if (!exists)
        {
            managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
            RemoveDefaultRule(subClient);
        }

        CreateRuleIfNotExists(eventName, subClient);

        return subClient;
    }

    /// <summary>
    /// This method will subscribe to the given event name and create a rule for it if it does not exist.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="subscriptionClient"></param>
    private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
    {
        bool ruleExists;

        try
        {
            var rule = managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
            ruleExists = rule != null;
        }
        catch (MessagingEntityNotFoundException)
        {
            // Azure Management Client doesn't  have RuleExists method
            ruleExists = false;
        }

        if (!ruleExists)
        {
            subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter
                {
                    Label = eventName
                },
                Name = eventName
            }).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// This method will remove the default rule from the subscription client.
    /// </summary>
    /// <param name="subscriptionClient"></param>
    private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
    {
        try
        {
            subscriptionClient
                 .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                 .GetAwaiter()
                 .GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            logger.LogWarning("The messaging entitiy {DefaultRuleName} Could not be found. ", RuleDescription.DefaultRuleName);
        }
    }

    /// <summary>
    /// This method will create a subscription client for the given event name.
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    private SubscriptionClient CreateSubscriptionClient(string eventName)
    {
        return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
    }

    /// <summary>
    /// Disposing the EventBus and closing the topic and management clients.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        topicClient.CloseAsync().GetAwaiter().GetResult();
        managementClient.CloseAsync().GetAwaiter().GetResult();
        topicClient = null;
        managementClient = null;
    }
}
