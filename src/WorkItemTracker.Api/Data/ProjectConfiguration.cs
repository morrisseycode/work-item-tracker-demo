using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Data;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100);

        builder.HasMany(p => p.WorkItems)
            .WithOne()
            .HasForeignKey(w => w.ProjectId);
    }
}
