using System.Net;

using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

// Each test uses its own user ids, because the limiter's counters live as long as the
// class fixture.
public class RateLimitingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const int PermitLimit = 100;

    [Fact]
    public async Task Request_over_the_per_user_limit_returns_429()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClientFor("rate-limited-user");

        for (var i = 0; i < PermitLimit; i++)
        {
            var allowed = await client.GetAsync("/api/projects", ct);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var response = await client.GetAsync("/api/projects", ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Limit_is_counted_separately_for_each_user()
    {
        var ct = TestContext.Current.CancellationToken;
        var busyClient = factory.CreateClientFor("busy-user");
        var otherClient = factory.CreateClientFor("other-user");

        for (var i = 0; i <= PermitLimit; i++)
        {
            await busyClient.GetAsync("/api/projects", ct);
        }

        var response = await otherClient.GetAsync("/api/projects", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_requests_return_401_without_using_up_a_quota()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        for (var i = 0; i < PermitLimit; i++)
        {
            await client.GetAsync("/api/projects", ct);
        }

        var response = await client.GetAsync("/api/projects", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
