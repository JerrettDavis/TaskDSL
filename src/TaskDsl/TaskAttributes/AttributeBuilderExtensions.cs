namespace TaskDsl.TaskAttributes;

public class AttributeBuilder
{
    private List<string> Parts { get; } = new();
    
    public int AttributeCount => Parts.Count;

    public AttributeBuilder AddIf(bool condition, string value)
    {
        if (condition) Parts.Add(value);
        return this;
    }
    
    
    public AttributeBuilder AddIf(bool condition, Func<string> valueGenerator)
    {
        if (condition) Parts.Add(valueGenerator());
        return this;
    }

    public AttributeBuilder AddRangeFluent(IEnumerable<string> values)
    {
        Parts.AddRange(values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        return this;
    }

    public AttributeBuilder AddRangeOrdered(
        IEnumerable<string> values,
        Func<string, string> formatter)
    {
        Parts.AddRange(values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(formatter));
        return this;
    }

    public AttributeBuilder AddRangeOrderedKV(
        IEnumerable<KeyValuePair<string, string>> values,
        Func<KeyValuePair<string, string>, string> formatter)
    {
        Parts.AddRange(values.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).Select(formatter));
        return this;
    }

    public string BuildCanonicalAttributes(string separator = " ")
        => string.Join(separator, Parts);
}