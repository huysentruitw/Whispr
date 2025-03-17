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

## Outbox

The outbox pattern is implemented using EF Core. The outbox is used to ensure that messages are sent when related state has been stored, even if the application crashes or is restarted. The outbox is implemented using the `Pigeon.EntityFrameworkCore` package.

_TODO_
