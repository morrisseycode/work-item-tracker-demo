using System.Net.Http.Json;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Domain;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class ListEfficiencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task List_uses_the_same_number_of_sql_commands_for_5_and_50_rows()
    {
        var fiveRowCommands = await CountListCommandsAsync("five-rows-owner", 5);
        var fiftyRowCommands = await CountListCommandsAsync("fifty-rows-owner", 50);

        Assert.Equal(fiveRowCommands, fiftyRowCommands);
    }

    private async Task<int> CountListCommandsAsync(string ownerId, int rowCount)
    {
        var ct = TestContext.Current.CancellationToken;
        var workItems = TestData.NewWorkItems(ownerId, rowCount).ToList();
        foreach (var workItem in workItems)
        {
            workItem.Assignee = new Assignee { DisplayName = $"Assignee for {workItem.Title}" };
        }

        await factory.SeedAsync(TestData.NewProject(ownerId, workItems));
        var client = factory.CreateClientFor(ownerId);

        factory.SqlCommands.Reset();
        var items = await client.GetFromJsonAsync<List<WorkItemResponse>>("/api/workitems", ct);
        var commands = factory.SqlCommands.Count;

        Assert.NotEqual(0, commands);
        Assert.Equal(rowCount, items!.Count);
        Assert.All(items, i => Assert.NotNull(i.AssigneeName));
        return commands;
    }
}
