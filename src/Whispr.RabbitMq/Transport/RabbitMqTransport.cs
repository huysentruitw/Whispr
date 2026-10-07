using Microsoft.Extensions.Logging;
using Whispr.RabbitMq.Management;

namespace Whispr.RabbitMq.Transport;

internal sealed partial class RabbitMqTransport(
    ConnectionProvider connectionProvider,
    PublishChannelPool publishChannelPool,
    TopologyManager topologyManager,
    RabbitMqOptions options,
    ILogger<RabbitMqTransport> logger) : ITransport
{
    private const string MessageTypeHeaderName = "MessageType";
    private const string ContentType = "application/json";
}
