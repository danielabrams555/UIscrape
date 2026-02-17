namespace UIScrape.Models;

public class InteractionResult
{
    public string MethodName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string TimestampString => Timestamp.ToString("HH:mm:ss.fff");

    public string Summary
    {
        get
        {
            var param = string.IsNullOrEmpty(Parameter) ? "" : $" ({Parameter})";
            var status = Success ? "OK" : "FAIL";
            return $"[{TimestampString}] [{status}] {MethodName}{param}: {Message}";
        }
    }
}

public class InteractionMethod
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresParameter { get; set; }
    public string ParameterHint { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string UnavailableReason { get; set; } = string.Empty;

    public string DisplayName => IsAvailable ? Name : $"{Name} (unavailable)";

    public string ToolTip => IsAvailable
        ? Description
        : $"{Description}\n\nUnavailable: {UnavailableReason}";
}
