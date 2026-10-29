using System.Net;
using System.Text;

using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class EnumValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "enum-owner";

    // Undefined numbers, defined numbers, numbers as strings, and combined names.
    // "Low, High" and "InProgress, Completed" combine to a defined member (High, Cancelled).
    public static TheoryData<string> PriorityValuesThatAreNotNames =>
        ["99", "\"99\"", "-1", "\"-1\"", "2", "\"2\"", "\"Low, High\""];

    public static TheoryData<string> StatusQueryValuesThatAreNotNames =>
        ["99", "-1", "1", "New,InProgress", "InProgress,Completed", "InProgress, Completed"];

    [Theory]
    [MemberData(nameof(PriorityValuesThatAreNotNames))]
    public async Task Create_with_a_priority_that_is_not_a_defined_name_returns_400(
        string priority)
    {
        var project = TestData.NewProject(Owner);
        await factory.SeedAsync(project);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/workitems",
            $$"""{ "projectId": {{project.Id}}, "title": "Plan", "priority": {{priority}} }""");

        AssertBadRequest(priority, response);
    }

    [Theory]
    [MemberData(nameof(PriorityValuesThatAreNotNames))]
    public async Task Update_with_a_priority_that_is_not_a_defined_name_returns_400(
        string priority)
    {
        var workItem = TestData.NewWorkItem(Owner);
        await factory.SeedAsync(TestData.NewProject(Owner, workItem));

        var response = await SendAsync(
            HttpMethod.Put,
            $"/api/workitems/{workItem.Id}",
            $$"""{ "title": "Plan", "priority": {{priority}} }""");

        AssertBadRequest(priority, response);
    }

    [Theory]
    [MemberData(nameof(StatusQueryValuesThatAreNotNames))]
    public async Task List_status_filter_that_is_not_a_defined_name_returns_400(string status)
    {
        var response = await factory.CreateClientFor(Owner).GetAsync(
            $"/api/workitems?status={Uri.EscapeDataString(status)}",
            TestContext.Current.CancellationToken);

        AssertBadRequest(status, response);
    }

    [Fact]
    public async Task List_status_filter_accepts_a_defined_name_in_any_letter_case()
    {
        var response = await factory.CreateClientFor(Owner).GetAsync(
            "/api/workitems?status=inprogress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string json)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        return factory.CreateClientFor(Owner)
            .SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void AssertBadRequest(string value, HttpResponseMessage response) =>
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for {value} but got {(int)response.StatusCode}.");
}
