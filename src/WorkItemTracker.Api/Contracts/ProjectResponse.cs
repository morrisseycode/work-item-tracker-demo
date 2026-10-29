using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Contracts;

public record ProjectResponse(int Id, string Name)
{
    public static ProjectResponse FromEntity(Project project) => new(project.Id, project.Name);
}
