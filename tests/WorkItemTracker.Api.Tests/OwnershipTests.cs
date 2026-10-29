using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class OwnershipTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("GET", "/api/projects")]
    [InlineData("POST", "/api/projects")]
    [InlineData("GET", "/api/assignees")]
    [InlineData("GET", "/api/workitems")]
    [InlineData("GET", "/api/workitems/1")]
    [InlineData("POST", "/api/workitems")]
    [InlineData("PUT", "/api/workitems/1")]
    public async Task Request_without_a_token_returns_401(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/projects")]
    [InlineData("GET", "/api/workitems")]
    [InlineData("GET", "/api/workitems/1")]
    public async Task Signed_token_without_a_sub_claim_returns_401(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Authorization = new("Bearer", TestTokens.WithoutSubject());

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_a_token_gets_a_problem_details_body()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await factory.CreateClient().GetAsync("/api/workitems", ct);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
        Assert.Equal(401, problem!.Status);
    }

    [Fact]
    public async Task User_sees_only_their_own_projects()
    {
        var ct = TestContext.Current.CancellationToken;
        var alice = factory.CreateClientFor("alice-projects");
        var bob = factory.CreateClientFor("bob-projects");
        await alice.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Alpha"), ct);
        await bob.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Beta"), ct);

        var projects = await alice.GetFromJsonAsync<List<ProjectResponse>>("/api/projects", ct);

        var project = Assert.Single(projects!);
        Assert.Equal("Alpha", project.Name);
    }

    [Fact]
    public async Task Creating_a_project_sets_the_owner_from_the_token()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClientFor("carol-owner");
        var body = new { name = "Gamma", ownerId = "someone-else" };

        var response = await client.PostAsJsonAsync("/api/projects", body, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProjectResponse>(ct);
        var ownerId = await factory.QueryDbAsync(db => db.Projects
            .Where(p => p.Id == created!.Id)
            .Select(p => p.OwnerId)
            .SingleAsync(ct));
        Assert.Equal("carol-owner", ownerId);
    }

    [Fact]
    public async Task Owner_can_read_their_work_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = await SeedWorkItemAsync("dana-read");

        var response = await factory.CreateClientFor("dana-read")
            .GetFromJsonAsync<WorkItemResponse>($"/api/workitems/{workItem.Id}", ct);

        Assert.Equal(workItem.Title, response!.Title);
    }

    [Fact]
    public async Task Another_user_gets_404_reading_a_work_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = await SeedWorkItemAsync("erin-read");

        var response = await factory.CreateClientFor("frank-read")
            .GetAsync($"/api/workitems/{workItem.Id}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Another_users_work_item_and_a_missing_one_get_the_same_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = await SeedWorkItemAsync("gina-read");
        var client = factory.CreateClientFor("hank-read");

        var forbidden = await client.GetAsync($"/api/workitems/{workItem.Id}", ct);
        var missing = await client.GetAsync("/api/workitems/999999", ct);

        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var forbiddenProblem = await forbidden.Content.ReadFromJsonAsync<ProblemBody>(ct);
        var missingProblem = await missing.Content.ReadFromJsonAsync<ProblemBody>(ct);
        Assert.Equal(missingProblem, forbiddenProblem);
    }

    [Fact]
    public async Task Work_item_list_returns_only_the_callers_items()
    {
        var ct = TestContext.Current.CancellationToken;
        var mine = await SeedWorkItemAsync("ivy-list");
        await SeedWorkItemAsync("jack-list");

        var items = await factory.CreateClientFor("ivy-list")
            .GetFromJsonAsync<List<WorkItemResponse>>("/api/workitems", ct);

        Assert.Equal([mine.Id], items!.Select(i => i.Id));
    }

    [Fact]
    public async Task Owner_can_update_their_work_item()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = await SeedWorkItemAsync("kim-update");

        var response = await factory.CreateClientFor("kim-update").PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}", new UpdateWorkItemRequest("Renamed"), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed", await TitleInDbAsync(workItem.Id));
    }

    [Fact]
    public async Task Another_user_gets_404_updating_a_work_item_and_it_is_unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = await SeedWorkItemAsync("liam-update");

        var response = await factory.CreateClientFor("mona-update").PutAsJsonAsync(
            $"/api/workitems/{workItem.Id}", new UpdateWorkItemRequest("Hijacked"), ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(workItem.Title, await TitleInDbAsync(workItem.Id));
    }

    [Fact]
    public async Task Creating_a_work_item_in_another_users_project_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var project = TestData.NewProject("quinn-project");
        await factory.SeedAsync(project);

        var response = await factory.CreateClientFor("ravi-create").PostAsJsonAsync(
            "/api/workitems", new CreateWorkItemRequest(project.Id, "Sneaky item"), ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var created = await factory.QueryDbAsync(db =>
            db.WorkItems.AnyAsync(w => w.ProjectId == project.Id, ct));
        Assert.False(created);
    }

    private Task<string> TitleInDbAsync(int workItemId) =>
        factory.QueryDbAsync(db => db.WorkItems
            .Where(w => w.Id == workItemId)
            .Select(w => w.Title)
            .SingleAsync());

    private async Task<WorkItem> SeedWorkItemAsync(string ownerId)
    {
        var workItem = TestData.NewWorkItem(ownerId);
        await factory.SeedAsync(TestData.NewProject(ownerId, workItem));
        return workItem;
    }

    // The ProblemDetails fields that must not reveal whether the item exists.
    private sealed record ProblemBody(string? Type, string? Title, int? Status, string? Detail);
}
