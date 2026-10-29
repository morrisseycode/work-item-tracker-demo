using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public partial class ValidationAndPagingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "validation-owner";

    // Replaced with the id of a project the caller owns, so only the named field is wrong.
    private const string ProjectIdToken = "PROJECT_ID";

    private static readonly string TooLongTitle = new('t', 201);
    private static readonly string TooLongDescription = new('d', 2001);

    public static TheoryData<string, string> InvalidCreateBodies => new()
    {
        { "empty title", """{ "projectId": PROJECT_ID, "title": "" }""" },
        { "blank title", """{ "projectId": PROJECT_ID, "title": "   " }""" },
        {
            "title too long",
            $$"""{ "projectId": PROJECT_ID, "title": "{{TooLongTitle}}" }"""
        },
        {
            "description too long",
            $$"""
            { "projectId": PROJECT_ID, "title": "Plan", "description": "{{TooLongDescription}}" }
            """
        },
        { "missing project", """{ "title": "Plan" }""" },
        {
            "unknown priority",
            """{ "projectId": PROJECT_ID, "title": "Plan", "priority": "Urgent" }"""
        },
        {
            "priority out of range",
            """{ "projectId": PROJECT_ID, "title": "Plan", "priority": 99 }"""
        },
        {
            "bad due date",
            """{ "projectId": PROJECT_ID, "title": "Plan", "dueDate": "2026-13-01" }"""
        },
    };

    [Fact]
    public async Task Missing_title_returns_400()
    {
        var response = await PostWorkItemAsync($$"""{ "projectId": {{ProjectIdToken}} }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidCreateBodies))]
    public async Task Create_with_a_missing_or_out_of_range_field_returns_400(
        string field, string json)
    {
        var response = await PostWorkItemAsync(json);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for {field} but got {(int)response.StatusCode}.");
    }

    public static TheoryData<string, string> InvalidUpdateBodies => new()
    {
        { "blank title", """{ "title": "  " }""" },
        { "title too long", $$"""{ "title": "{{TooLongTitle}}" }""" },
        {
            "description too long",
            $$"""{ "title": "Plan", "description": "{{TooLongDescription}}" }"""
        },
        { "unknown priority", """{ "title": "Plan", "priority": "Urgent" }""" },
        { "bad due date", """{ "title": "Plan", "dueDate": "2026-02-30" }""" },
    };

    public static TheoryData<string, string> InvalidProjectBodies => new()
    {
        { "missing name", "{}" },
        { "empty name", """{ "name": "" }""" },
        { "name too long", $$"""{ "name": "{{new string('n', 101)}}" }""" },
    };

    [Fact]
    public async Task Validation_errors_use_problem_details()
    {
        var response = await PostWorkItemAsync($$"""{ "projectId": {{ProjectIdToken}} }""");

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(InvalidUpdateBodies))]
    public async Task Update_with_an_out_of_range_field_returns_400(string field, string json)
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner)
            .PutAsync($"/api/workitems/{workItem.Id}", Json(json), ct);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for {field} but got {(int)response.StatusCode}.");
    }

    [Theory]
    [MemberData(nameof(InvalidProjectBodies))]
    public async Task Create_project_with_a_missing_or_too_long_name_returns_400(
        string field, string json)
    {
        var response = await factory.CreateClientFor(Owner)
            .PostAsync("/api/projects", Json(json), TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for {field} but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task Update_without_a_title_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var workItem = TestData.NewWorkItem(Owner);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await factory.CreateClientFor(Owner)
            .PutAsync($"/api/workitems/{workItem.Id}", Json("""{ "priority": "High" }"""), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PostWorkItemAsync(string json)
    {
        var project = TestData.NewProject(Owner);
        await factory.SeedAsync(project);
        var body = json.Replace(ProjectIdToken, $"{project.Id}", StringComparison.Ordinal);

        return await factory.CreateClientFor(Owner)
            .PostAsync("/api/workitems", Json(body), TestContext.Current.CancellationToken);
    }

    private static StringContent Json(string json) =>
        new(json, Encoding.UTF8, "application/json");

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    public async Task Out_of_range_paging_parameters_return_400(string queryString)
    {
        var response = await factory.CreateClientFor("paging-validation")
            .GetAsync($"/api/workitems?{queryString}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Page_size_of_100_is_allowed()
    {
        var response = await factory.CreateClientFor("paging-max")
            .GetAsync("/api/workitems?pageSize=100", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_50_items_by_default()
    {
        var items = await SeedAndListAsync("paging-default", 60, "/api/workitems");

        Assert.Equal(50, items.Count);
    }

    [Fact]
    public async Task Page_and_page_size_select_a_slice_ordered_by_id()
    {
        var items = await SeedAndListAsync(
            "paging-slice", 5, "/api/workitems?page=2&pageSize=2");

        Assert.Equal(["Task 3", "Task 4"], items.Select(i => i.Title));
    }

    [Fact]
    public async Task List_sql_orders_by_id()
    {
        var ct = TestContext.Current.CancellationToken;
        const string owner = "paging-sql";
        await factory.SeedAsync(TestData.NewProject(owner, TestData.NewWorkItems(owner, 3)));
        var client = factory.CreateClientFor(owner);

        factory.SqlCommands.Reset();
        await client.GetAsync("/api/workitems?page=2&pageSize=2", ct);

        var listCommand = Assert.Single(
            factory.SqlCommands.Texts,
            t => t.Contains("FROM \"WorkItems\"", StringComparison.Ordinal));
        Assert.Matches(OrderByIdOnly(), listCommand);
    }

    // ORDER BY on the Id column alone, ascending: not descending and not after another key.
    [GeneratedRegex("""ORDER BY "\w+"\."Id"(?!\s+DESC)(?!,)""")]
    private static partial Regex OrderByIdOnly();

    private async Task<List<WorkItemResponse>> SeedAndListAsync(
        string ownerId, int count, string url)
    {
        await factory.SeedAsync(
            TestData.NewProject(ownerId, TestData.NewWorkItems(ownerId, count)));
        var items = await factory.CreateClientFor(ownerId)
            .GetFromJsonAsync<List<WorkItemResponse>>(url, TestContext.Current.CancellationToken);
        return items!;
    }
}
