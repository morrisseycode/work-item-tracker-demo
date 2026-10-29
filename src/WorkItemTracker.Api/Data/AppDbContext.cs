using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Assignee> Assignees => Set<Assignee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
