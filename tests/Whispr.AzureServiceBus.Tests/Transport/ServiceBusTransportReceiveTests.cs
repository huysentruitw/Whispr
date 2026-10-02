using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Whispr.AzureServiceBus.Conventions;
using Whispr.AzureServiceBus.Management;
using Whispr.AzureServiceBus.Transport;

namespace Whispr.AzureServiceBus.Tests.Transport;

public sealed class ServiceBusTransportReceiveTests
{
    [Fact]
    public void Given_NewOptions_Then_BackgroundCompletionIsOffWithSixteenPending()
    {
        // Act
        var options = new AzureServiceBusOptions();

        // Assert
        Assert.False(options.CompleteMessagesInBackground);
        Assert.Equal(16, options.MaxPendingCompletions);
    }

    [Fact]
    public async Task Given_DefaultOptions_When_StartListener_Then_PrefetchEqualsConcurrency()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { QueueConcurrencyLimit = 4 });

        // Act
        await harness.StartListener();

        // Assert
        Assert.NotNull(harness.ProcessorOptions);
        Assert.Equal(4, harness.ProcessorOptions.PrefetchCount);
        Assert.Equal(4, harness.ProcessorOptions.MaxConcurrentCalls);
    }

    [Fact]
    public async Task Given_CompleteMessagesInBackground_When_StartListener_Then_PrefetchEqualsConcurrency()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { QueueConcurrencyLimit = 4, CompleteMessagesInBackground = true, MaxPendingCompletions = 8 });

        // Act
        await harness.StartListener();

        // Assert
        Assert.NotNull(harness.ProcessorOptions);
        Assert.Equal(4, harness.ProcessorOptions.PrefetchCount);
        Assert.Equal(4, harness.ProcessorOptions.MaxConcurrentCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Given_MaxPendingCompletionsNotPositive_When_StartListener_Then_Throws(int maxPendingCompletions)
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true, MaxPendingCompletions = maxPendingCompletions });

        // Act
        var exception = await Record.ExceptionAsync(async () => await harness.StartListener());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
        Assert.Equal(0, harness.QueueExistsCalls);
    }

    [Fact]
    public async Task Given_DefaultOptions_When_MessageHandled_Then_WaitsForTheCompletion()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions());
        await harness.StartListener();
        var completion = new TaskCompletionSource();
        var args = harness.CreateArgs("message-1", completion.Task);

        // Act
        var delivery = harness.Processor.Deliver(args);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var returnedBeforeCompletion = delivery.IsCompleted;
        completion.SetResult();
        await delivery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(returnedBeforeCompletion);
    }

    [Fact]
    public async Task Given_CompleteMessagesInBackground_When_MessageHandled_Then_ReturnsBeforeTheCompletionFinishes()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        await harness.StartListener();
        var completion = new TaskCompletionSource();
        var args = harness.CreateArgs("message-1", completion.Task);

        // Act
        await harness.Processor.Deliver(args).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        harness.Receiver.Verify(x => x.CompleteMessageAsync(args.Message, CancellationToken.None), Times.Once);
        completion.SetResult();
    }

    [Fact]
    public async Task Given_LockAboutToExpire_When_MessageHandled_Then_WaitsForTheCompletion()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        await harness.StartListener();
        var completion = new TaskCompletionSource();
        var args = harness.CreateArgs("message-1", completion.Task, lockedFor: TimeSpan.FromSeconds(30));

        // Act
        var delivery = harness.Processor.Deliver(args);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var returnedBeforeCompletion = delivery.IsCompleted;
        completion.SetResult();
        await delivery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(returnedBeforeCompletion);
    }

    [Fact]
    public async Task Given_ExpiredLock_When_MessageReceived_Then_SkipsTheHandlerWithoutSettling()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        var handled = false;
        await harness.StartListener((_, _) =>
        {
            handled = true;
            return ValueTask.CompletedTask;
        });
        var args = harness.CreateArgs("message-1", Task.CompletedTask, lockedFor: TimeSpan.FromSeconds(-10));

        // Act
        await harness.Processor.Deliver(args).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(handled);
        harness.Receiver.Verify(x => x.AbandonMessageAsync(args.Message, It.IsAny<IDictionary<string, object>>(), CancellationToken.None), Times.Never);
        harness.Receiver.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Given_BackgroundCompletionFails_When_TheLockIsStillHeld_Then_AbandonsTheMessage()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        await harness.StartListener();
        var args = harness.CreateArgs("message-1", Task.FromException(new ServiceBusException("timeout", ServiceBusFailureReason.ServiceTimeout)));

        // Act
        await harness.Processor.Deliver(args).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await harness.Transport.StopListeners(TestContext.Current.CancellationToken);

        // Assert
        harness.Receiver.Verify(x => x.AbandonMessageAsync(args.Message, It.IsAny<IDictionary<string, object>>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Given_BackgroundCompletionFails_When_TheLockIsLost_Then_DoesNotAbandon()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        await harness.StartListener();
        var args = harness.CreateArgs("message-1", Task.FromException(new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost)));

        // Act
        await harness.Processor.Deliver(args).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await harness.Transport.StopListeners(TestContext.Current.CancellationToken);

        // Assert
        harness.Receiver.Verify(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Given_HandlerStillRunning_When_StopListeners_Then_WaitsForItsBackgroundCompletion()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        var handlerGate = new TaskCompletionSource();
        await harness.StartListener(async (_, _) => await handlerGate.Task);
        var completion = new TaskCompletionSource();
        _ = harness.Processor.Deliver(harness.CreateArgs("message-1", completion.Task));

        // Act
        var stop = harness.Transport.StopListeners(TestContext.Current.CancellationToken).AsTask();
        handlerGate.SetResult();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var stoppedBeforeCompletion = stop.IsCompleted;
        completion.SetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(stoppedBeforeCompletion);
    }

    [Fact]
    public async Task Given_PendingBackgroundCompletion_When_DisposeAsync_Then_WaitsForIt()
    {
        // Arrange
        var harness = new Harness(new AzureServiceBusOptions { CompleteMessagesInBackground = true });
        await harness.StartListener();
        var completion = new TaskCompletionSource();
        await harness.Processor.Deliver(harness.CreateArgs("message-1", completion.Task));

        // Act
        var dispose = harness.Transport.DisposeAsync().AsTask();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var disposedBeforeCompletion = dispose.IsCompleted;
        completion.SetResult();
        await dispose.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(disposedBeforeCompletion);
    }

    private sealed class Harness
    {
        public Harness(AzureServiceBusOptions options)
        {
            var client = new Mock<ServiceBusClient>();
            client
                .Setup(x => x.CreateProcessor(It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>()))
                .Callback<string, ServiceBusProcessorOptions>((_, processorOptions) => ProcessorOptions = processorOptions)
                .Returns(Processor);

            var administrationClient = new Mock<ServiceBusAdministrationClient>();
            administrationClient
                .Setup(x => x.QueueExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => QueueExistsCalls++)
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var namingConvention = new Mock<ISubscriptionNamingConvention>();
            namingConvention.Setup(x => x.Format(It.IsAny<string>())).Returns<string>(name => name);

            Receiver
                .Setup(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Transport = new ServiceBusTransport(
                new SenderFactory(client.Object),
                new ProcessorFactory(client.Object),
                new EntityManager(administrationClient.Object, new QueueCreationOptions(), new TopicCreationOptions()),
                namingConvention.Object,
                options,
                NullLogger<ServiceBusTransport>.Instance);
        }

        public FakeProcessor Processor { get; } = new();

        public Mock<ServiceBusReceiver> Receiver { get; } = new();

        public ServiceBusProcessorOptions? ProcessorOptions { get; private set; }

        public int QueueExistsCalls { get; private set; }

        public ServiceBusTransport Transport { get; }

        public ValueTask StartListener(Func<SerializedEnvelope, CancellationToken, ValueTask>? messageCallback = null)
            => Transport.StartListener("queue", [], messageCallback ?? ((_, _) => ValueTask.CompletedTask), TestContext.Current.CancellationToken);

        public ProcessMessageEventArgs CreateArgs(string messageId, Task completion, TimeSpan? lockedFor = null)
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString("{}"),
                messageId: messageId,
                properties: new Dictionary<string, object> { ["MessageType"] = "Type" },
                lockedUntil: DateTimeOffset.UtcNow + (lockedFor ?? TimeSpan.FromMinutes(5)));
            Receiver
                .Setup(x => x.CompleteMessageAsync(message, CancellationToken.None))
                .Returns(completion);
            return new ProcessMessageEventArgs(message, Receiver.Object, CancellationToken.None);
        }
    }

    private sealed class FakeProcessor : ServiceBusProcessor
    {
        private readonly List<Task> _inFlight = [];

        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => Task.WhenAll(_inFlight);

        public Task Deliver(ProcessMessageEventArgs args)
        {
            var task = OnProcessMessageAsync(args);
            _inFlight.Add(task);
            return task;
        }
    }
}
