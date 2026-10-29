using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Data;

public class AssigneeConfiguration : IEntityTypeConfiguration<Assignee>
{
    public void Configure(EntityTypeBuilder<Assignee> builder) =>
        builder.Property(a => a.DisplayName).HasMaxLength(100);
}
