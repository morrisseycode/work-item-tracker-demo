using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class UpdateAssigneeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "assignee-owner";

    [Fact]
    public async Task Put_assignee_reassigns_and_returns_the_previous_assignee_name()
    {
        var ct = TestContext.Current.CancellationToken;
        var previous = new Assignee { DisplayName = "Lena Fischer" };
        var next = new Assignee { DisplayName = "Tariq Haddad" };
        var workItem = TestData.NewWorkItem(Owner, assignee: previous);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem), next);

        var response = await factory.CreateClientFor(Owner).PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}/assignee",
            new UpdateWorkItemAssigneeRequest(next.Id),
            ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WorkItemAssigneeChangeResponse>(ct);
        Assert.Equal("Lena Fischer", body!.PreviousAssigneeName);
    }

    [Fact]
    public async Task Put_null_assignee_unassigns_the_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Jonas Berg" };
        var workItem = TestData.NewWorkItem(Owner, assignee: assignee);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}/assignee",
            new UpdateWorkItemAssigneeRequest(null),
            ct);

        var body = await response.Content.ReadFromJsonAsync<WorkItemAssigneeChangeResponse>(ct);
        Assert.Equal("Jonas Berg", body!.PreviousAssigneeName);
    }

    [Fact]
    public async Task Put_unknown_assignee_id_returns_400_and_changes_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Sofia Marques" };
        var workItem = TestData.NewWorkItem(Owner, assignee: assignee);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner).PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}/assignee",
            new UpdateWorkItemAssigneeRequest(999999),
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await factory.QueryDbAsync(db =>
            db.WorkItems.AsNoTracking().SingleAsync(w => w.Id == workItem.Id, ct));
        Assert.Equal(assignee.Id, stored.AssigneeId);
    }

    [Fact]
    public async Task Put_assignee_of_another_users_item_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var assignee = new Assignee { DisplayName = "Noah Clarke" };
        var workItem = TestData.NewWorkItem("someone-else");
        await factory.SeedAsync(TestData.NewProject("someone-else", workItem), assignee);

        var response = await factory.CreateClientFor(Owner).PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}/assignee",
            new UpdateWorkItemAssigneeRequest(assignee.Id),
            ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
