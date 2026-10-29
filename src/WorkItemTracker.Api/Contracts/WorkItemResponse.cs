using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Contracts;

public record WorkItemResponse(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    DateOnly? DueDate,
    int? AssigneeId,
    string? AssigneeName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    // The work item's Assignee navigation must be loaded for AssigneeName to be set.
    public static WorkItemResponse FromEntity(WorkItem workItem) => new(
        workItem.Id,
        workItem.ProjectId,
        workItem.Title,
        workItem.Description,
        workItem.Status,
        workItem.Priority,
        workItem.DueDate,
        workItem.AssigneeId,
        workItem.Assignee?.DisplayName,
        workItem.CreatedAt,
        workItem.CompletedAt);
}
