using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Tests.Infrastructure;

public static class TestData
{
    public static Project NewProject(string ownerId, params IEnumerable<WorkItem> workItems)
    {
        var project = new Project { Name = "Quarterly planning", OwnerId = ownerId };
        foreach (var workItem in workItems)
        {
            project.WorkItems.Add(workItem);
        }

        return project;
    }

    public static WorkItem NewWorkItem(
        string ownerId,
        string title = "Review the budget",
        WorkItemStatus status = WorkItemStatus.New,
        DateOnly? dueDate = null,
        Assignee? assignee = null) => new()
        {
            Title = title,
            OwnerId = ownerId,
            Status = status,
            DueDate = dueDate,
            Assignee = assignee,
            CreatedAt = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero),
        };

    public static IEnumerable<WorkItem> NewWorkItems(string ownerId, int count) =>
        Enumerable.Range(1, count).Select(i => NewWorkItem(ownerId, $"Task {i}"));
}
