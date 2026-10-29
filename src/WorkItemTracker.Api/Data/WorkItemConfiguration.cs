using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Data;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.Property(w => w.Title).HasMaxLength(200);
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.Status).HasConversion<string>();
        builder.Property(w => w.Priority).HasConversion<string>();

        builder.HasOne(w => w.Assignee)
            .WithMany()
            .HasForeignKey(w => w.AssigneeId);
    }
}
