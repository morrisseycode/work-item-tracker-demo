using System.ComponentModel.DataAnnotations;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Contracts;

public record UpdateWorkItemRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description = null,
    [EnumDataType(typeof(WorkItemPriority))]
    WorkItemPriority Priority = WorkItemPriority.Medium,
    DateOnly? DueDate = null,
    int? AssigneeId = null);
