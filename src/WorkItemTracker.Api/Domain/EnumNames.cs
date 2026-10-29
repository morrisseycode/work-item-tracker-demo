namespace WorkItemTracker.Api.Domain;

public static class EnumNames
{
    // Matches exactly one defined name, in any letter case. Unlike Enum.TryParse, numbers
    // and comma-separated names such as "Low, High" do not match.
    public static bool TryParse<TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum
    {
        var name = Enum.GetNames<TEnum>()
            .FirstOrDefault(n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase));

        value = name is null ? default : Enum.Parse<TEnum>(name);
        return name is not null;
    }
}
