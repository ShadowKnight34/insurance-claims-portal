using ClaimsPortal.Claims.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClaimsPortal.Claims.Infrastructure.Events;

/// <summary>
/// In-process stand-in for a broker. The application publishes claim status changes
/// after they are saved. A Kafka producer can replace this class without changing
/// the claim workflow.
/// </summary>
public sealed class LoggingClaimEventPublisher : IClaimEventPublisher
{
    private readonly ILogger<LoggingClaimEventPublisher> _logger;

    public LoggingClaimEventPublisher(ILogger<LoggingClaimEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(ClaimStatusChanged change, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Claim {Reference} moved from {FromStatus} to {ToStatus}",
            change.ReferenceNumber,
            change.FromStatus ?? "(new)",
            change.ToStatus);
        return Task.CompletedTask;
    }
}
