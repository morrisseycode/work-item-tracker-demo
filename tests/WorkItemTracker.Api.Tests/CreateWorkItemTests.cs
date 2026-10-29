using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class CreateWorkItemTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "create-owner";

    [Fact]
    public async Task Create_returns_201_with_a_location_header_for_the_new_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClientFor(Owner);
        var project = await SeedProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/workitems", new CreateWorkItemRequest(project.Id, "Draft the agenda"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        var fetched = await client.GetFromJsonAsync<WorkItemResponse>(
            response.Headers.Location, ct);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task New_item_gets_status_new_and_owner_and_created_time_from_the_server()
    {
        var ct = TestContext.Current.CancellationToken;
        var project = await SeedProjectAsync();
        var body = new
        {
            projectId = project.Id,
            title = "Book the venue",
            status = "Completed",
            ownerId = "someone-else",
            createdAt = "2030-01-01T00:00:00Z",
            completedAt = "2030-01-02T00:00:00Z",
        };

        var response = await factory.CreateClientFor(Owner)
            .PostAsJsonAsync("/api/workitems", body, ct);

        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == created!.Id, ct));
        Assert.Equal(WorkItemStatus.New, stored.Status);
        Assert.Equal(Owner, stored.OwnerId);
        Assert.Equal(factory.Time.UtcNow, stored.CreatedAt);
        Assert.Null(stored.CompletedAt);
    }

    [Fact]
    public async Task Create_stores_every_field_from_the_body()
    {
        var ct = TestContext.Current.CancellationToken;
        var project = await SeedProjectAsync();
        var assignee = new Assignee { DisplayName = "Priya Raman" };
        await factory.SeedAsync(assignee);
        var request = new CreateWorkItemRequest(
            project.Id,
            "Book the venue",
            "Confirm capacity for forty people.",
            WorkItemPriority.High,
            new DateOnly(2026, 4, 9),
            assignee.Id);

        var response = await factory.CreateClientFor(Owner)
            .PostAsJsonAsync("/api/workitems", request, ct);

        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == created!.Id, ct));
        Assert.Equal(project.Id, stored.ProjectId);
        Assert.Equal("Book the venue", stored.Title);
        Assert.Equal("Confirm capacity for forty people.", stored.Description);
        Assert.Equal(WorkItemPriority.High, stored.Priority);
        Assert.Equal(new DateOnly(2026, 4, 9), stored.DueDate);
        Assert.Equal(assignee.Id, stored.AssigneeId);
    }

    [Fact]
    public async Task Priority_defaults_to_medium()
    {
        var ct = TestContext.Current.CancellationToken;
        var project = await SeedProjectAsync();

        var response = await factory.CreateClientFor(Owner).PostAsJsonAsync(
            "/api/workitems", new { projectId = project.Id, title = "Order supplies" }, ct);

        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Equal(WorkItemPriority.Medium, created!.Priority);
    }

    private async Task<Project> SeedProjectAsync()
    {
        var project = TestData.NewProject(Owner);
        await factory.SeedAsync(project);
        return project;
    }
}
