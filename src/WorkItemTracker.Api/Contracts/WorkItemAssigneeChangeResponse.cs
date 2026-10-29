namespace WorkItemTracker.Api.Contracts;

public record WorkItemAssigneeChangeResponse(
    WorkItemResponse WorkItem,
    string? PreviousAssigneeName);
