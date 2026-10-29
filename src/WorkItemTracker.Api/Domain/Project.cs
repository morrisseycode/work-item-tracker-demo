namespace WorkItemTracker.Api.Domain;

public class Project
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string OwnerId { get; set; }

    public ICollection<WorkItem> WorkItems { get; } = [];
}
