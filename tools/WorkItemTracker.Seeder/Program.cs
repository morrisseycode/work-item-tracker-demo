using System.Diagnostics;
using System.Globalization;

using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Data;
using WorkItemTracker.Seeder;

const int DefaultWorkItemCount = 200;

var workItemCount = DefaultWorkItemCount;
if (args is ["--work-items", var value])
{
    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out workItemCount))
    {
        Console.Error.WriteLine("--work-items must be a whole number of zero or more.");
        return 1;
    }
}
else if (args.Length > 0)
{
    Console.Error.WriteLine("Usage: WorkItemTracker.Seeder [--work-items <count>]");
    return 1;
}

var databasePath = Path.Combine(
    FindRepositoryRoot(), "src", "WorkItemTracker.Api", "workitemtracker.db");
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;

await using var db = new AppDbContext(options);

var stopwatch = Stopwatch.StartNew();
await new DatabaseSeeder(db).ResetAndSeedAsync(workItemCount, CancellationToken.None);
stopwatch.Stop();

Console.WriteLine(
    $"Seeded {workItemCount} work items into {databasePath} "
    + $"in {stopwatch.Elapsed.TotalSeconds:F1} seconds.");
return 0;

// The API resolves its relative database path against its project directory, so the
// seeder finds that directory from the repository root rather than the current directory.
static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null
        && !File.Exists(Path.Combine(directory.FullName, "WorkItemTracker.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName
        ?? throw new InvalidOperationException("Could not find WorkItemTracker.slnx.");
}
