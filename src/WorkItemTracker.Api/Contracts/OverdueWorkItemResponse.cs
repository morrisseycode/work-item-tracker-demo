namespace WorkItemTracker.Api.Contracts;

public record OverdueWorkItemResponse(
    int Id,
    string Title,
    DateOnly DueDate,
    int DaysOverdue,
    string? AssigneeName);
