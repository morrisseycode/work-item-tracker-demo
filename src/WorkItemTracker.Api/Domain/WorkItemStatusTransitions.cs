namespace WorkItemTracker.Api.Domain;

public static class WorkItemStatusTransitions
{
    private static readonly Dictionary<WorkItemStatus, WorkItemStatus[]> Allowed = new()
    {
        [WorkItemStatus.New] = [WorkItemStatus.InProgress, WorkItemStatus.Cancelled],
        [WorkItemStatus.InProgress] =
            [WorkItemStatus.New, WorkItemStatus.Completed, WorkItemStatus.Cancelled],
        [WorkItemStatus.Completed] = [WorkItemStatus.InProgress],
        [WorkItemStatus.Cancelled] = [],
    };

    // A change to the status the item already has is never allowed.
    public static bool CanChange(WorkItemStatus from, WorkItemStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
