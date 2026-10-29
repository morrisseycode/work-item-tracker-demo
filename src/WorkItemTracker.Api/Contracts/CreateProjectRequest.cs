using System.ComponentModel.DataAnnotations;

namespace WorkItemTracker.Api.Contracts;

public record CreateProjectRequest([Required, StringLength(100)] string Name);
