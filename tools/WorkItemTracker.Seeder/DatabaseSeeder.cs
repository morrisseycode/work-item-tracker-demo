using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Data;
using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Seeder;

public class DatabaseSeeder(AppDbContext db)
{
    private const int RandomSeed = 20261006;
    private const int BatchSize = 5_000;
    private const double UnassignedShare = 0.15;
    private const double NoDueDateShare = 0.10;
    private const double DescriptionShare = 0.60;

    private static readonly DateTimeOffset EarliestCreatedAt =
        new(2025, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static readonly WorkItemStatus[] Statuses = Enum.GetValues<WorkItemStatus>();
    private static readonly WorkItemPriority[] Priorities = Enum.GetValues<WorkItemPriority>();

    private readonly Random random = new(RandomSeed);

    public async Task ResetAndSeedAsync(int workItemCount, CancellationToken cancellationToken)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        var projects = SampleData.ProjectNames
            .Select((name, i) => new Project
            {
                Name = name,
                OwnerId = SampleData.UserIds[i % SampleData.UserIds.Length],
            })
            .ToList();
        var assignees = SampleData.AssigneeNames
            .Select(name => new Assignee { DisplayName = name })
            .ToList();

        db.Projects.AddRange(projects);
        db.Assignees.AddRange(assignees);
        await db.SaveChangesAsync(cancellationToken);

        await InsertWorkItemsAsync(workItemCount, projects, assignees, cancellationToken);
    }

    private async Task InsertWorkItemsAsync(
        int workItemCount,
        List<Project> projects,
        List<Assignee> assignees,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        for (var inserted = 0; inserted < workItemCount; inserted += BatchSize)
        {
            var batchCount = Math.Min(BatchSize, workItemCount - inserted);
            for (var i = 0; i < batchCount; i++)
            {
                db.WorkItems.Add(NewWorkItem(projects, assignees));
            }

            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private WorkItem NewWorkItem(List<Project> projects, List<Assignee> assignees)
    {
        var project = Pick(projects);
        var status = Pick(Statuses);
        var createdAt = EarliestCreatedAt.AddMinutes(random.Next(0, 365 * 24 * 60));

        return new WorkItem
        {
            ProjectId = project.Id,
            OwnerId = project.OwnerId,
            Title = $"{Pick(SampleData.TitleVerbs)} {Pick(SampleData.TitleSubjects)}",
            Description = Chance(DescriptionShare) ? Pick(SampleData.Descriptions) : null,
            Status = status,
            Priority = Pick(Priorities),
            DueDate = Chance(NoDueDateShare)
                ? null
                : DateOnly.FromDateTime(createdAt.UtcDateTime).AddDays(random.Next(3, 120)),
            AssigneeId = Chance(UnassignedShare) ? null : Pick(assignees).Id,
            CreatedAt = createdAt,
            CompletedAt = status == WorkItemStatus.Completed
                ? createdAt.AddHours(random.Next(1, 30 * 24))
                : null,
        };
    }

    private T Pick<T>(IReadOnlyList<T> items) => items[random.Next(items.Count)];

    private bool Chance(double share) => random.NextDouble() < share;
}
