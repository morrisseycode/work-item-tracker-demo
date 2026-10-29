using System.Text.Json.Serialization;

namespace WorkItemTracker.Api.Domain;

[JsonConverter(typeof(EnumNameConverter<WorkItemPriority>))]
public enum WorkItemPriority
{
    Low,
    Medium,
    High,
}
