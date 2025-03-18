# Pigeon Messaging Library

A lightweight message bus implementation for Azure Service Bus with EF Core outbox.

## Example usage

```csharp
services
    .AddPigeon()
        .AddAzureServiceBusTransport(options =>
        {
            options.ConnectionString =
                configuration.GetValue<string>("AzureServiceBus:ConnectionString")
                ?? throw new InvalidOperationException("AzureServiceBus:ConnectionString is required");
        })
        .AddTopicNamingConvention<DefaultTopicNamingConvention>()
        .AddQueueNamingConvention<DefaultQueueNamingConvention>()
        .AddSubscriptionNamingConvention<SubscriptionNamingConvention>()
        .AddMessageHandlersFromAssembly(Assembly.GetExecutingAssembly());
```

## Naming conventions

This library auto-generates topics and queues by using the given naming conventions. The default naming conventions are:

- `DefaultTopicNamingConvention`: `MyNamespace.SomethingHappened` -> `my-namespace--something-happened`
- `DefaultQueueNamingConvention`: `MyNamespace.SomethingHappenedHandler` -> `my-namespace--something-happened-handler`

To customize the naming conventions, implement the following interfaces:

```csharp
public sealed class MyTopicNamingConvention : ITopicNamingConvention
{
    public string Format(Type messageType) => $"topic-{messageType.Name}";
}
```

```csharp
public sealed class MyQueueNamingConvention : IQueueNamingConvention
{
    public string Format(Type handlerType) => $"queue-{handlerType.Name}";
}
```

and register them using:

```csharp
services
    .AddPigeon()
        .AddTopicNamingConvention<MyTopicNamingConvention>()
        .AddQueueNamingConvention<MyQueueNamingConvention>();
```

## Transports

### In-memory

_TODO_

### Azure Service Bus

The Azure Service Bus transport is implemented using the `Microsoft.Azure.ServiceBus` package. The transport can be configured using the `AddAzureServiceBusTransport` method:

```csharp
services
    .AddPigeon()
        .AddAzureServiceBusTransport(options =>
        {
            options.ConnectionString =
                configuration.GetValue<string>("AzureServiceBus:ConnectionString")
                ?? throw new InvalidOperationException("AzureServiceBus:ConnectionString is required");
        });
```

Since this transport also creates subscriptions to forward messages from topics to queues, the subscription naming convention can be customized as well:

```csharp
public sealed class MySubscriptionNamingConvention : ISubscriptionNamingConvention
{
    public string Format(Type handlerType) => $"subscription-{handlerType.Name}";
}
```

and registered using:

```csharp
services
    .AddPigeon()
        .AddSubscriptionNamingConvention<MySubscriptionNamingConvention>();
```

## Filters

Pipeline:

```plaintext
+-----------------+   +----------------+   +-----------+   +----------------+   +-----------------+
| Message Publish |-->| Publish Filter |-->| Transport |-->| Consume Filter |-->| Message Handler |
+-----------------+   +----------------+   +-----------+   +----------------+   +-----------------+
```

Three types of filters can be applied to the messaging pipeline:

- `IPublishFilter`: Filters that are applied when a message is published.
- `ISendFilter`: Filters that are applied after the published message has been serialized but before it is sent.
- `IConsumeFilter`: Filters that are applied before the message is handled by the message handler.

## Outbox

The outbox pattern is implemented using EF Core. The outbox is a table in the database that stores messages that have been published. The outbox is used to prevent duplicate messages from being sent when a message is published multiple times.

Since the outbox is implemented as a `ISendFilter` to store published messages in the database, the outbox send filter must be added as the last send filter in the pipeline:

```csharp

The outbox is then polled periodically to send messages that have not been sent yet.

The outbox can be configured using the `AddOutboxSendFilter` method:

```csharp
services
    .AddPigeon()
        .AddSendFilter<MySendFilter>()
        .AddOutboxSendFilter();
```
