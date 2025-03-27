using Whispr.Bus;
using System.Text.Json;
using Whispr.Filtering;
using Whispr.Tests.TestInfrastructure;

namespace Whispr.Tests.Bus;

public sealed class MessageProcessorTests
{
    [Fact]
    public async Task Given_NoConsumeFilters_When_Process_Then_CallsHandlerDirectly()
    {
        // Arrange
        var testHarness = TestHarness<TestHandler, TestMessage>.Create([]);
        var message = new TestMessage("Test");
        var serializedEnvelope = SerializedEnvelopeFactory.Create(message);

        // Act
        await testHarness.Processor.Process(serializedEnvelope, CancellationToken.None);

        // Assert
        Assert.Equal(1, testHarness.Handler.HandleCallCount);
        Assert.Equal(message.Text, testHarness.Handler.LastHandledMessage?.Text);
    }

    [Fact]
    public async Task Given_ConsumeFilters_When_Process_Then_AppliesFiltersInReverseOrder()
    {
        // Arrange
        var filterCallOrder = new List<string>();
        var filter1 = new TestConsumeFilter("Filter1", filterCallOrder);
        var filter2 = new TestConsumeFilter("Filter2", filterCallOrder);

        var testHarness = TestHarness<TestHandler, TestMessage>.Create([filter1, filter2]);

        var message = new TestMessage("Test");
        var serializedEnvelope = SerializedEnvelopeFactory.Create(message);

        // Act
        await testHarness.Processor.Process(serializedEnvelope, CancellationToken.None);

        // Assert
        Assert.Equal(1, testHarness.Handler.HandleCallCount);
        Assert.Equal(["Filter1", "Filter2"], filterCallOrder);
    }

    [Fact]
    public async Task Given_InvalidJson_When_Process_Then_ThrowsException()
    {
        // Arrange
        var testHarness = TestHarness<TestHandler, TestMessage>.Create([]);

        var serializedEnvelope = new SerializedEnvelope
        {
            Body = "invalid json",
            MessageType = typeof(TestMessage).FullName!,
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            DeferredUntil = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            testHarness.Processor.Process(serializedEnvelope, CancellationToken.None).AsTask());
    }

    private sealed class TestHarness<THandler, TMessage>
        where THandler : IMessageHandler<TMessage>, new()
        where TMessage : class
    {
        public static TestHarness<THandler, TMessage> Create(IEnumerable<IConsumeFilter> filters)
        {
            var handler = new THandler();
            var processor = new MessageProcessor<THandler, TMessage>(filters, handler, new NoOpDiagnosticsEventListener());

            return new TestHarness<THandler, TMessage>
            {
                Processor = processor,
                Handler = handler,
            };
        }

        public required IMessageProcessor Processor { get; init; }

        public required THandler Handler { get; init; }
    }

    private sealed class TestHandler : IMessageHandler<TestMessage>
    {
        public int HandleCallCount { get; private set; }

        public TestMessage? LastHandledMessage { get; private set; }

        public ValueTask Handle(Envelope<TestMessage> envelope, CancellationToken cancellationToken)
        {
            HandleCallCount++;
            LastHandledMessage = envelope.Message;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestConsumeFilter(string name, List<string> callOrder) : IConsumeFilter
    {
        public async ValueTask Consume<TMessage>(
            Envelope<TMessage> envelope,
            Func<Envelope<TMessage>, CancellationToken, ValueTask> next,
            CancellationToken cancellationToken)
            where TMessage : class
        {
            callOrder.Add(name);
            await next(envelope, cancellationToken);
        }
    }

    private sealed record TestMessage(string Text);
}
