namespace UIScrape.Models;

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public IntPtr MainWindowHandle { get; set; }

    public string DisplayName => string.IsNullOrEmpty(WindowTitle)
        ? $"{ProcessName} (PID: {ProcessId})"
        : $"{WindowTitle} - {ProcessName} (PID: {ProcessId})";

    public override string ToString() => DisplayName;
}
