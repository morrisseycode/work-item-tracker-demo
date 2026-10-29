using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkItemTracker.Api.Domain;

[JsonConverter(typeof(EnumNameConverter<WorkItemStatus>))]
[TypeConverter(typeof(EnumNameTypeConverter<WorkItemStatus>))]
public enum WorkItemStatus
{
    New,
    InProgress,
    Completed,
    Cancelled,
}
