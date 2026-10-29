using System.ComponentModel.DataAnnotations;

using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Contracts;

public record UpdateWorkItemStatusRequest(
    [Required, EnumDataType(typeof(WorkItemStatus))] WorkItemStatus? Status);
