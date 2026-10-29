using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class AssigneeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Assignee_list_returns_all_assignees_ordered_by_display_name()
    {
        var ct = TestContext.Current.CancellationToken;
        await factory.SeedAsync(
            new Assignee { DisplayName = "Zofia Brandt" },
            new Assignee { DisplayName = "Arash Karimi" },
            new Assignee { DisplayName = "Mia Laurent" });

        var assignees = await factory.CreateClientFor("assignee-list")
            .GetFromJsonAsync<List<AssigneeResponse>>("/api/assignees", ct);

        var allNames = await factory.QueryDbAsync(db =>
            db.Assignees.Select(a => a.DisplayName).ToListAsync(ct));
        Assert.Equal(
            allNames.Order(StringComparer.Ordinal), assignees!.Select(a => a.DisplayName));
    }

    [Fact]
    public async Task Unassigned_work_item_has_null_assignee_fields_in_the_list()
    {
        var ct = TestContext.Current.CancellationToken;
        await factory.SeedAsync(
            TestData.NewProject("unassigned-list", TestData.NewWorkItem("unassigned-list")));

        var items = await factory.CreateClientFor("unassigned-list")
            .GetFromJsonAsync<List<WorkItemResponse>>("/api/workitems", ct);

        var item = Assert.Single(items!);
        Assert.Null(item.AssigneeId);
        Assert.Null(item.AssigneeName);
    }

    [Fact]
    public async Task Unassigned_work_item_has_null_assignee_fields_when_read_by_id()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem("unassigned-single");
        await factory.SeedAsync(TestData.NewProject("unassigned-single", workItem));

        var item = await factory.CreateClientFor("unassigned-single")
            .GetFromJsonAsync<WorkItemResponse>($"/api/workitems/{workItem.Id}", ct);

        Assert.Null(item!.AssigneeId);
        Assert.Null(item.AssigneeName);
    }

    [Fact]
    public async Task Assigned_work_item_includes_the_assignee_id_and_name()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Amara Okafor" };
        var workItem = TestData.NewWorkItem("assigned-single", assignee: assignee);
        await factory.SeedAsync(TestData.NewProject("assigned-single", workItem));

        var item = await factory.CreateClientFor("assigned-single")
            .GetFromJsonAsync<WorkItemResponse>($"/api/workitems/{workItem.Id}", ct);

        Assert.Equal(assignee.Id, item!.AssigneeId);
        Assert.Equal("Amara Okafor", item.AssigneeName);
    }

    [Fact]
    public async Task Creating_with_an_assignee_returns_the_assignee_name()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Kenji Watanabe" };
        var project = TestData.NewProject("assign-create");
        await factory.SeedAsync(project, assignee);

        var response = await factory.CreateClientFor("assign-create").PostAsJsonAsync(
            "/api/workitems",
            new CreateWorkItemRequest(project.Id, "Prepare slides", AssigneeId: assignee.Id),
            ct);

        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ct);
        Assert.Equal("Kenji Watanabe", created!.AssigneeName);
    }

    [Fact]
    public async Task Creating_with_an_unknown_assignee_id_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var project = TestData.NewProject("unknown-create");
        await factory.SeedAsync(project);

        var response = await factory.CreateClientFor("unknown-create").PostAsJsonAsync(
            "/api/workitems",
            new CreateWorkItemRequest(project.Id, "Prepare slides", AssigneeId: 999999),
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Updating_with_an_unknown_assignee_id_returns_400_and_changes_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem("unknown-update");
        await factory.SeedAsync(TestData.NewProject("unknown-update", workItem));

        var response = await factory.CreateClientFor("unknown-update").PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}",
            new UpdateWorkItemRequest("Changed title", AssigneeId: 999999),
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var title = await factory.QueryDbAsync(db => db.WorkItems
            .Where(w => w.Id == workItem.Id)
            .Select(w => w.Title)
            .SingleAsync(ct));
        Assert.Equal(workItem.Title, title);
    }
}
