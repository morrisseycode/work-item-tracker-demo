using System.Net.Http.Json;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class ListFilterTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Project_filter_returns_only_items_in_that_project()
    {
        const string owner = "project-filter";
        var first = TestData.NewProject(owner, TestData.NewWorkItem(owner, "In first"));
        var second = TestData.NewProject(owner, TestData.NewWorkItem(owner, "In second"));
        await factory.SeedAsync(first, second);

        var titles = await ListTitlesAsync(owner, $"/api/workitems?projectId={second.Id}");

        Assert.Equal(["In second"], titles);
    }

    [Fact]
    public async Task Status_filter_returns_only_items_with_that_status()
    {
        const string owner = "status-filter";
        await factory.SeedAsync(TestData.NewProject(
            owner,
            TestData.NewWorkItem(owner, "Not started", WorkItemStatus.New),
            TestData.NewWorkItem(owner, "Under way", WorkItemStatus.InProgress)));

        var titles = await ListTitlesAsync(owner, "/api/workitems?status=InProgress");

        Assert.Equal(["Under way"], titles);
    }

    private async Task<IEnumerable<string>> ListTitlesAsync(string ownerId, string url)
    {
        var items = await factory.CreateClientFor(ownerId)
            .GetFromJsonAsync<List<WorkItemResponse>>(url, TestContext.Current.CancellationToken);
        return items!.Select(i => i.Title);
    }
}
