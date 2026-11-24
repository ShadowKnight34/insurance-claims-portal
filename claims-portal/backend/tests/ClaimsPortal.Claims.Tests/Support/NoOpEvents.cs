using ClaimsPortal.Claims.Application.Abstractions;

namespace ClaimsPortal.Claims.Tests.Support;

public sealed class NoOpEvents : IClaimEventPublisher
{
    public List<ClaimStatusChanged> Published { get; } = [];

    public Task PublishAsync(ClaimStatusChanged change, CancellationToken cancellationToken)
    {
        Published.Add(change);
        return Task.CompletedTask;
    }
}
