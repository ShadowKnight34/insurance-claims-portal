using ClaimsPortal.Claims.Application.Abstractions;

namespace ClaimsPortal.Claims.Tests.Support;

public sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}
