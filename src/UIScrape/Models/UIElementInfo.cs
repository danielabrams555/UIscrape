using System.Collections.ObjectModel;

namespace UIScrape.Models;

public class UIElementInfo
{
    public string Name { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string LocalizedControlType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsOffscreen { get; set; }
    public System.Windows.Rect BoundingRectangle { get; set; }
    public string Value { get; set; } = string.Empty;
    public int ChildCount { get; set; }
    public int Depth { get; set; }

    public ObservableCollection<UIElementInfo> Children { get; set; } = new();

    public string DisplayName
    {
        get
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(LocalizedControlType))
                parts.Add(LocalizedControlType);
            else if (!string.IsNullOrEmpty(ControlType))
                parts.Add(ControlType);

            if (!string.IsNullOrEmpty(Name))
                parts.Add($"\"{Name}\"");
            else if (!string.IsNullOrEmpty(AutomationId))
                parts.Add($"[{AutomationId}]");

            return parts.Count > 0 ? string.Join(" - ", parts) : "(Unnamed Element)";
        }
    }

    public string Details
    {
        get
        {
            var lines = new List<string>
            {
                $"Control Type: {ControlType}",
                $"Localized Type: {LocalizedControlType}",
                $"Name: {(string.IsNullOrEmpty(Name) ? "(none)" : Name)}",
                $"Automation ID: {(string.IsNullOrEmpty(AutomationId) ? "(none)" : AutomationId)}",
                $"Class Name: {(string.IsNullOrEmpty(ClassName) ? "(none)" : ClassName)}",
                $"Is Enabled: {IsEnabled}",
                $"Is Offscreen: {IsOffscreen}",
                $"Bounding Rect: {BoundingRectangle}",
                $"Children: {ChildCount}"
            };

            if (!string.IsNullOrEmpty(Value))
                lines.Add($"Value: {Value}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
