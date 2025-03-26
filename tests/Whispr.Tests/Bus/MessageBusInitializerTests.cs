using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Whispr.Bus;
using Whispr.Conventions;
using Whispr.Tests.TestInfrastructure;
using Whispr.Transport;

namespace Whispr.Tests.Bus;

public sealed class MessageBusInitializerTests
{
    [Fact]
    public async Task Given_NoHandlerDescriptors_When_StartInitializer_Then_DoesntStartListener()
    {
        // Arrange
        var testHarness = TestHarness.Create([]);

        // Act
        await testHarness.Initializer.Start(TestContext.Current.CancellationToken);

        // Assert
        testHarness.Transport.Verify(
            x => x.StartListener(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Func<SerializedEnvelope,CancellationToken,ValueTask>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Given_HandlerDescriptors_When_StartInitializer_Then_StartsListeners()
    {
        // Arrange
        var descriptors = new[]
        {
            new MessageHandlerDescriptor { HandlerType = typeof(HandlerOne), MessageTypes = [ typeof(MessageOne) ] },
            new MessageHandlerDescriptor { HandlerType = typeof(HandlerTwo), MessageTypes = [ typeof(MessageOne), typeof(MessageTwo) ] },
        };
        var testHarness = TestHarness.Create(descriptors);

        // Act
        await testHarness.Initializer.Start(TestContext.Current.CancellationToken);

        // Assert
        testHarness.Transport.Verify(
            x => x.StartListener("queue-HandlerOne", new[] { "topic-MessageOne" }, It.IsAny<Func<SerializedEnvelope,CancellationToken,ValueTask>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        testHarness.Transport.Verify(
            x => x.StartListener("queue-HandlerTwo", new[] { "topic-MessageOne", "topic-MessageTwo" }, It.IsAny<Func<SerializedEnvelope,CancellationToken,ValueTask>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestHarness
    {
        public static TestHarness Create(IEnumerable<MessageHandlerDescriptor> descriptors)
        {
            var transportMock = new Mock<ITransport>();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var initializer = new MessageBusInitializer(
                descriptors,
                CreateQueueNamingConvention(),
                CreateTopicNamingConvention(),
                transportMock.Object,
                serviceProvider,
                new NoOpDiagnosticsEventListener(),
                Mock.Of<ILogger<MessageBusInitializer>>());

            return new TestHarness { Initializer = initializer, Transport = transportMock };
        }

        public required MessageBusInitializer Initializer { get; internal init; }

        public required Mock<ITransport> Transport { get; internal init; }

        private static IQueueNamingConvention CreateQueueNamingConvention()
        {
            var queueNamingConventionMock = new Mock<IQueueNamingConvention>();
            queueNamingConventionMock
                .Setup(x => x.Format(It.IsAny<Type>()))
                .Returns<Type>(handlerType => $"queue-{handlerType.Name}");
            return queueNamingConventionMock.Object;
        }

        private static ITopicNamingConvention CreateTopicNamingConvention()
        {
            var topicNamingConventionMock = new Mock<ITopicNamingConvention>();
            topicNamingConventionMock
                .Setup(x => x.Format(It.IsAny<Type>()))
                .Returns<Type>(handlerType => $"topic-{handlerType.Name}");
            return topicNamingConventionMock.Object;
        }
    }

    private sealed class HandlerOne();

    private sealed class HandlerTwo();

    private sealed record MessageOne(string Text);

    private sealed record MessageTwo(string Text);
}
