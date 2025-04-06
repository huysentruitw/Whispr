using Microsoft.Extensions.DependencyInjection;
using Whispr.Bus;
using Whispr.Filtering;
using Whispr.Tests.TestInfrastructure;
using Whispr.Transport;

namespace Whispr.Tests.Bus;

public sealed class MessageSenderTests
{
    [Fact]
    public async Task Given_NoSendFilters_When_Send_Then_SendsMessageDirectlyToTransport()
    {
        // Arrange
        var topicName = "t/test";
        var serializedEnvelope = SerializedEnvelopeFactory.Create(new TestMessage("Test content"));
        var testHarness = TestHarness.Create([]);

        // Act
        await testHarness.Sender.Send(topicName, serializedEnvelope, CancellationToken.None);

        // Assert
        testHarness.Transport.Verify(
            x => x.Send(topicName, serializedEnvelope, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_SendFilters_When_Send_Then_AppliesFiltersInRegistrationOrder()
    {
        // Arrange
        var topicName = "t/test";
        var serializedEnvelope = SerializedEnvelopeFactory.Create(new TestMessage("Test content"));
       
        var callOrder = new List<string>();
        var filter1 = new TestSendFilter((_, _) => callOrder.Add("Filter1"));
        var filter2 = new TestSendFilter((_, _) => callOrder.Add("Filter2"));
    
        var testHarness = TestHarness.Create([filter1, filter2]);
        testHarness.Transport.Setup(x => x.Send(topicName, serializedEnvelope, CancellationToken.None))
            .Callback(() => callOrder.Add("Transport"));
    
        // Act
        await testHarness.Sender.Send(topicName, serializedEnvelope, CancellationToken.None);
    
        // Assert
        Assert.Equal(["Filter1", "Filter2", "Transport"], callOrder);
    }

    private sealed class TestHarness
    {
        public static TestHarness Create(IEnumerable<ISendFilter> filters)
        {
            var transportMock = new Mock<ITransport>();
            var services = new ServiceCollection();
            foreach (var filter in filters)
                services.AddScoped(_ => filter);
            
            var sender = new MessageSender(
                services.BuildServiceProvider(),
                transportMock.Object,
                diagnosticEventListener: new NoOpDiagnosticsEventListener());

            return new TestHarness
            {
                Sender = sender,
                Transport = transportMock,
            };
        }
        
        public required MessageSender Sender { get; internal init; }
        
        public required Mock<ITransport> Transport { get; internal init; }
    }
    
    private sealed class TestSendFilter(Action<string, SerializedEnvelope> sendAction) : ISendFilter
    {
        public async ValueTask Send(
            string topicName,
            SerializedEnvelope envelope,
            Func<string, SerializedEnvelope, CancellationToken, ValueTask> next,
            CancellationToken cancellationToken)
        {
            sendAction(topicName, envelope);
            await next(topicName, envelope, cancellationToken);
        }
    }

    private sealed record TestMessage(string Content);
}