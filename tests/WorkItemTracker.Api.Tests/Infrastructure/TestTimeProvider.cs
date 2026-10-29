namespace WorkItemTracker.Api.Tests.Infrastructure;

public sealed class TestTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } =
        new(2026, 3, 14, 10, 30, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
