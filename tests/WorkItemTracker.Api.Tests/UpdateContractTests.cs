using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class UpdateContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Put_changes_every_editable_field()
    {
        var ct = TestContext.Current.CancellationToken;
        var before = new Assignee { DisplayName = "Noah Fischer" };
        var after = new Assignee { DisplayName = "Aisha Rahman" };
        var workItem = TestData.NewWorkItem(
            "edit-owner", dueDate: new DateOnly(2026, 1, 9), assignee: before);
        await factory.SeedAsync(TestData.NewProject("edit-owner", workItem), after);
        var request = new UpdateWorkItemRequest(
            "New title",
            "New description",
            WorkItemPriority.Low,
            new DateOnly(2026, 12, 31),
            after.Id);

        var response = await factory.CreateClientFor("edit-owner")
            .PutAsJsonAsync($"/api/workitems/{workItem.Id}", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal("New title", stored.Title);
        Assert.Equal("New description", stored.Description);
        Assert.Equal(WorkItemPriority.Low, stored.Priority);
        Assert.Equal(new DateOnly(2026, 12, 31), stored.DueDate);
        Assert.Equal(after.Id, stored.AssigneeId);
    }

    [Fact]
    public async Task Put_with_null_assignee_unassigns_the_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Ingrid Solberg" };
        var workItem = TestData.NewWorkItem("unassign-owner", assignee: assignee);
        await factory.SeedAsync(TestData.NewProject("unassign-owner", workItem));

        var response = await factory.CreateClientFor("unassign-owner").PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}", new UpdateWorkItemRequest("Same title"), ct);

        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Null(body!.AssigneeId);
        Assert.Null(body.AssigneeName);
    }

    [Fact]
    public async Task Put_ignores_owner_status_project_and_timestamps_in_the_body()
    {
        var ct = TestContext.Current.CancellationToken;
        var createdAt = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
        var completedAt = new DateTimeOffset(2026, 1, 20, 17, 0, 0, TimeSpan.Zero);
        var workItem = TestData.NewWorkItem("update-owner", status: WorkItemStatus.Completed);
        workItem.CreatedAt = createdAt;
        workItem.CompletedAt = completedAt;
        var otherProject = TestData.NewProject("update-owner");
        await factory.SeedAsync(TestData.NewProject("update-owner", workItem), otherProject);

        var body = new
        {
            title = "Updated title",
            ownerId = "someone-else",
            status = "Cancelled",
            projectId = otherProject.Id,
            createdAt = "2030-01-01T00:00:00Z",
            completedAt = "2030-01-02T00:00:00Z",
        };
        var response = await factory.CreateClientFor("update-owner")
            .PutAsJsonAsync($"/api/workitems/{workItem.Id}", body, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal("Updated title", stored.Title);
        Assert.Equal("update-owner", stored.OwnerId);
        Assert.Equal(WorkItemStatus.Completed, stored.Status);
        Assert.Equal(workItem.ProjectId, stored.ProjectId);
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.Equal(completedAt, stored.CompletedAt);
    }
}
