namespace WorkItemTracker.Api.Domain;

public class WorkItem
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.New;

    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;

    public DateOnly? DueDate { get; set; }

    public int? AssigneeId { get; set; }

    public Assignee? Assignee { get; set; }

    public required string OwnerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
