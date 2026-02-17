using System.Collections.ObjectModel;
using System.Windows.Automation;

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

    // Properties for element identification
    public string TreePath { get; set; } = string.Empty;
    public int SiblingIndex { get; set; }
    public int[] RuntimeId { get; set; } = Array.Empty<int>();
    public string ProcessName { get; set; } = string.Empty;

    // Supported automation patterns detected during scan
    public List<string> SupportedPatterns { get; set; } = new();

    // Reference to the underlying AutomationElement for interaction
    // This may become stale if the target app's UI changes
    public AutomationElement? AutomationElementRef { get; set; }

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

    public string RuntimeIdString => RuntimeId.Length > 0
        ? string.Join(".", RuntimeId)
        : "(none)";

    public string SupportedPatternsString => SupportedPatterns.Count > 0
        ? string.Join(", ", SupportedPatterns)
        : "(none)";

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
                $"Runtime ID: {RuntimeIdString}",
                $"Sibling Index: {SiblingIndex}",
                $"Is Enabled: {IsEnabled}",
                $"Is Offscreen: {IsOffscreen}",
                $"Bounding Rect: {BoundingRectangle}",
                $"Children: {ChildCount}",
                $"Supported Patterns: {SupportedPatternsString}"
            };

            if (!string.IsNullOrEmpty(Value))
                lines.Add($"Value: {Value}");

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// Generates a C# code snippet to find this element using UI Automation
    /// </summary>
    public string FinderCodeSnippet
    {
        get
        {
            var conditions = new List<string>();

            // Prefer AutomationId if available
            if (!string.IsNullOrEmpty(AutomationId))
            {
                conditions.Add($"new PropertyCondition(AutomationElement.AutomationIdProperty, \"{AutomationId}\")");
            }

            // Add ControlType
            conditions.Add($"new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.{ControlType})");

            // Add Name if available and no AutomationId
            if (!string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(AutomationId))
            {
                conditions.Add($"new PropertyCondition(AutomationElement.NameProperty, \"{EscapeString(Name)}\")");
            }

            if (conditions.Count == 1)
            {
                return $"element.FindFirst(TreeScope.Descendants,\n    {conditions[0]});";
            }
            else
            {
                return $"element.FindFirst(TreeScope.Descendants,\n    new AndCondition(\n        {string.Join(",\n        ", conditions)}));";
            }
        }
    }

    private static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
