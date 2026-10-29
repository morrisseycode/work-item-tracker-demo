using System.Globalization;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Tests.Infrastructure;

namespace WorkItemTracker.Api.Tests;

public class DueDateFilterTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Owner = "due-date-owner";

    // Single- and double-digit months and days, across month and year boundaries.
    private static readonly string[] DueDates =
    [
        "2025-12-31", "2026-01-01", "2026-01-09", "2026-01-10", "2026-02-01",
        "2026-09-30", "2026-10-01", "2026-10-10", "2026-11-05",
    ];

    [Theory]
    [InlineData("2026-01-01", new[] { "2025-12-31" })]
    [InlineData("2026-01-10", new[] { "2025-12-31", "2026-01-01", "2026-01-09" })]
    [InlineData("2026-02-01",
        new[] { "2025-12-31", "2026-01-01", "2026-01-09", "2026-01-10" })]
    [InlineData("2026-10-01", new[]
    {
        "2025-12-31", "2026-01-01", "2026-01-09", "2026-01-10", "2026-02-01", "2026-09-30",
    })]
    [InlineData("2026-10-10", new[]
    {
        "2025-12-31", "2026-01-01", "2026-01-09", "2026-01-10", "2026-02-01", "2026-09-30",
        "2026-10-01",
    })]
    public async Task Due_before_returns_items_due_strictly_before_the_date(
        string dueBefore, string[] expectedDueDates)
    {
        var dueDates = await GetDueDatesAsync($"/api/workitems?dueBefore={dueBefore}");

        Assert.Equal(expectedDueDates, dueDates);
    }

    [Fact]
    public async Task Items_without_a_due_date_never_match_due_before()
    {
        var dueDates = await GetDueDatesAsync("/api/workitems?dueBefore=9999-12-31");

        Assert.Equal(DueDates, dueDates);
    }

    [Fact]
    public async Task Due_before_is_applied_before_paging()
    {
        const string owner = "due-paging-owner";
        var early = new DateOnly(2026, 1, 15);
        var late = new DateOnly(2026, 3, 15);
        // Matching and non-matching items alternate, so filtering after paging
        // would return a short page.
        var workItems = Enumerable.Range(1, 50)
            .Select(i => TestData.NewWorkItem(
                owner, $"Item {i}", dueDate: i % 2 == 1 ? early : (i % 4 == 0 ? late : null)))
            .ToList();
        await factory.SeedAsync(TestData.NewProject(owner, workItems));

        var page = await factory.CreateClientFor(owner).GetFromJsonAsync<List<WorkItemResponse>>(
            "/api/workitems?dueBefore=2026-02-01&page=2&pageSize=10",
            TestContext.Current.CancellationToken);

        var expectedIds = workItems.Where(w => w.DueDate == early).Skip(10).Take(10)
            .Select(w => w.Id);
        Assert.Equal(expectedIds, page!.Select(i => i.Id));
    }

    private async Task<string?[]> GetDueDatesAsync(string url)
    {
        await SeedOnceAsync();
        var items = await factory.CreateClientFor(Owner)
            .GetFromJsonAsync<List<WorkItemResponse>>(url, TestContext.Current.CancellationToken);
        return items!.Select(i => i.DueDate is { } d ? FormatDate(d) : null).ToArray();
    }

    // Tests in one class share a database, so the data is seeded by whichever test runs first.
    private async Task SeedOnceAsync()
    {
        var alreadySeeded = await factory.QueryDbAsync(db =>
            db.WorkItems.AnyAsync(w => w.OwnerId == Owner));
        if (alreadySeeded)
        {
            return;
        }

        var workItems = DueDates
            .Select(d => TestData.NewWorkItem(Owner, $"Due {d}", dueDate: ParseDate(d)))
            .Append(TestData.NewWorkItem(Owner, "No due date"));
        await factory.SeedAsync(TestData.NewProject(Owner, workItems));
    }

    private static DateOnly ParseDate(string date) =>
        DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
