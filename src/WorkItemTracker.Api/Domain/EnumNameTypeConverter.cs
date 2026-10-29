using System.ComponentModel;
using System.Globalization;

namespace WorkItemTracker.Api.Domain;

// Used by MVC model binding (for example ?status=) so query values follow the same rule
// as JSON bodies: exactly one defined name, in any letter case.
public class EnumNameTypeConverter<TEnum>() : EnumConverter(typeof(TEnum))
    where TEnum : struct, Enum
{
    public override object? ConvertFrom(
        ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text && EnumNames.TryParse<TEnum>(text, out var result)
            ? result
            : throw new FormatException($"'{value}' is not a {typeof(TEnum).Name} name.");
}
