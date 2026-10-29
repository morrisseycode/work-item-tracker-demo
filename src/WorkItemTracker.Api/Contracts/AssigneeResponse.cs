using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Contracts;

public record AssigneeResponse(int Id, string DisplayName)
{
    public static AssigneeResponse FromEntity(Assignee assignee) =>
        new(assignee.Id, assignee.DisplayName);
}
