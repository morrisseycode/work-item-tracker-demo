using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class UpdateStatusTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "status-owner";

    [Fact]
    public async Task Patch_status_changes_the_status_and_returns_the_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Mateo Silva" };
        var workItem = TestData.NewWorkItem(Owner, assignee: assignee);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(WorkItemStatus.InProgress),
            ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Equal(WorkItemStatus.InProgress, body!.Status);
        Assert.Equal("Mateo Silva", body.AssigneeName);
        Assert.Null(body.CompletedAt);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal(WorkItemStatus.InProgress, stored.Status);
    }

    [Fact]
    public async Task Patch_status_to_completed_records_the_completion_time()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner, status: WorkItemStatus.InProgress);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(WorkItemStatus.Completed),
            ct);

        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Equal(factory.Time.UtcNow, body!.CompletedAt);
    }

    [Fact]
    public async Task Patch_status_to_completed_again_returns_400_and_keeps_the_completion_time()
    {
        var ct = TestContext.Current.CancellationToken;
        var completedAt = new DateTimeOffset(2026, 1, 20, 17, 0, 0, TimeSpan.Zero);
        var workItem = TestData.NewWorkItem(Owner, status: WorkItemStatus.Completed);
        workItem.CompletedAt = completedAt;
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(WorkItemStatus.Completed),
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal(completedAt, stored.CompletedAt);
    }

    [Theory]
    [InlineData(WorkItemStatus.New, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.New, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.New)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Completed)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.Completed, WorkItemStatus.InProgress)]
    public async Task Patch_status_with_an_allowed_change_saves_the_new_status(
        WorkItemStatus from,
        WorkItemStatus to)
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner, status: from);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(to),
            ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal(to, stored.Status);
    }

    [Theory]
    [InlineData(WorkItemStatus.New, WorkItemStatus.New)]
    [InlineData(WorkItemStatus.New, WorkItemStatus.Completed)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Completed, WorkItemStatus.New)]
    [InlineData(WorkItemStatus.Completed, WorkItemStatus.Completed)]
    [InlineData(WorkItemStatus.Completed, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.New)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.Completed)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.Cancelled)]
    public async Task Patch_status_with_a_disallowed_change_returns_400_and_keeps_the_status(
        WorkItemStatus from,
        WorkItemStatus to)
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner, status: from);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(to),
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal(from, stored.Status);
    }

    [Fact]
    public async Task Patch_status_away_from_completed_clears_the_completion_time()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner, status: WorkItemStatus.Completed);
        workItem.CompletedAt = new DateTimeOffset(2026, 1, 20, 17, 0, 0, TimeSpan.Zero);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(WorkItemStatus.InProgress),
            ct);

        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Null(body!.CompletedAt);
    }

    [Fact]
    public async Task Patch_status_of_another_users_item_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem("someone-else");
        await factory.SeedAsync(TestData.NewProject("someone-else", workItem));

        var response = await factory.CreateClientFor(Owner).PatchAsJsonAsync(
            $"/api/workitems/{workItem.Id}/status",
            new UpdateWorkItemStatusRequest(WorkItemStatus.Cancelled),
            ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "status": null }""")]
    [InlineData("""{ "status": 99 }""")]
    [InlineData("""{ "status": "Done" }""")]
    public async Task Patch_status_without_a_defined_status_returns_400(string json)
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var request = new HttpRequestMessage(
            HttpMethod.Patch, $"/api/workitems/{workItem.Id}/status")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        var response = await factory.CreateClientFor(Owner).SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
