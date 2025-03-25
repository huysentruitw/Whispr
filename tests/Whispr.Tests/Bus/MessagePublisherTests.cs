using Whispr.Bus;
using Whispr.Conventions;
using Whispr.Transport;
using Whispr.Filtering;
using Whispr.Outbox;

namespace Whispr.Tests.Bus;

public sealed class MessagePublisherTests
{
    [Fact]
    public async Task Given_NoPublishFilters_When_Publish_Then_SendsMessageDirectly()
    {
        // Arrange
        var testHarness = TestHarness.Create([]);
        var message = new TestMessage("Test content");

        // Act
        await testHarness.Publisher.Publish(message, null, CancellationToken.None);

        // Assert
        testHarness.MessageSender.Verify(
            x => x.Send(
                It.Is<string>(topic => topic == "topic-TestMessage"),
                It.Is<SerializedEnvelope>(env =>
                    env.MessageType == typeof(TestMessage).FullName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Given_PublishFilters_When_Publish_Then_AppliesFiltersInReverseOrder()
    {
        // Arrange
        var filterCallOrder = new List<string>();
        var filter1 = new TestPublishFilter("Filter1", filterCallOrder);
        var filter2 = new TestPublishFilter("Filter2", filterCallOrder);

        var testHarness = TestHarness.Create([filter1, filter2]);
        var message = new TestMessage("Test content");

        // Act
        await testHarness.Publisher.Publish(message, null, CancellationToken.None);

        // Assert
        Assert.Equal(["Filter1", "Filter2"], filterCallOrder);
    }

    [Fact]
    public async Task Given_OutboxAvailable_When_Publish_Then_AddsToOutbox()
    {
        // Arrange
        var testHarness = TestHarness.CreateWithOutbox();
        var message = new TestMessage("Test content");

        // Act
        await testHarness.Publisher.Publish(message, null, CancellationToken.None);

        // Assert
        testHarness.Outbox!.Verify(
            x => x.Add(
                It.Is<string>(topic => topic == "topic-TestMessage"),
                It.IsAny<SerializedEnvelope>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        testHarness.VerifyTransportNotUsed();
    }

    [Fact]
    public async Task Given_PublishOptions_When_Publish_Then_AppliesOptions()
    {
        // Arrange
        var testHarness = TestHarness.Create([]);
        var message = new TestMessage("Test content");
        var correlationId = Guid.NewGuid().ToString();
        var deferredUntil = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        await testHarness.Publisher.Publish(message, options =>
        {
            options.Correlate(correlationId);
            options.Defer(deferredUntil);
            options.Headers.Add("CustomHeader", "HeaderValue");
        }, CancellationToken.None);

        // Assert
        testHarness.MessageSender.Verify(
            x => x.Send(
                It.IsAny<string>(),
                It.Is<SerializedEnvelope>(env =>
                    env.CorrelationId == correlationId &&
                    env.DeferredUntil == deferredUntil),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestHarness
    {
        public static TestHarness Create(IEnumerable<IPublishFilter> filters)
        {
            var messageSenderMock = new Mock<IMessageSender>();
            var topicNamingConvention = CreateTopicNamingConvention();

            var publisher = new MessagePublisher(
                filters,
                topicNamingConvention,
                messageSenderMock.Object);

            return new TestHarness { Publisher = publisher, MessageSender = messageSenderMock, Outbox = null };
        }

        public static TestHarness CreateWithOutbox()
        {
            var messageSenderMock = new Mock<IMessageSender>();
            var outboxMock = new Mock<IOutbox>();
            var topicNamingConvention = CreateTopicNamingConvention();

            var publisher = new MessagePublisher(
                [],
                topicNamingConvention,
                messageSenderMock.Object,
                outboxMock.Object);

            return new TestHarness { Publisher = publisher, MessageSender = messageSenderMock, Outbox = outboxMock };
        }

        public required MessagePublisher Publisher { get; internal init; }
        public required Mock<IMessageSender> MessageSender { get; internal init; }
        public Mock<IOutbox>? Outbox { get; internal init; }

        public void VerifyTransportNotUsed()
        {
            MessageSender.Verify(
                x => x.Send(
                    It.IsAny<string>(),
                    It.IsAny<SerializedEnvelope>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static ITopicNamingConvention CreateTopicNamingConvention()
        {
            var convention = new Mock<ITopicNamingConvention>();
            convention
                .Setup(x => x.Format(It.IsAny<Type>()))
                .Returns<Type>(messageType => $"topic-{messageType.Name}");
            return convention.Object;
        }
    }

    private sealed class TestPublishFilter(string name, List<string> callOrder) : IPublishFilter
    {
        public async ValueTask Publish<TMessage>(
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
