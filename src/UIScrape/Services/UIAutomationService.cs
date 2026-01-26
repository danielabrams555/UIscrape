using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using UIScrape.Models;

namespace UIScrape.Services;

public class UIAutomationService
{
    private const int MaxDepth = 25;

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    public IEnumerable<ProcessInfo> GetWindowedProcesses()
    {
        var processes = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero &&
                    IsWindowVisible(process.MainWindowHandle) &&
                    !string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    processes.Add(new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = process.MainWindowTitle,
                        MainWindowHandle = process.MainWindowHandle
                    });
                }
            }
            catch
            {
                // Skip processes we can't access
            }
        }

        return processes.OrderBy(p => p.WindowTitle).ToList();
    }

    public async Task<ObservableCollection<UIElementInfo>> GetUIElementsAsync(
        IntPtr windowHandle,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var elements = new ObservableCollection<UIElementInfo>();

            try
            {
                var rootElement = AutomationElement.FromHandle(windowHandle);
                if (rootElement != null)
                {
                    var rootInfo = CreateElementInfo(rootElement, 0);
                    PopulateChildren(rootElement, rootInfo, 1, cancellationToken);
                    elements.Add(rootInfo);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Return empty collection on error
                System.Diagnostics.Debug.WriteLine($"Error getting UI elements: {ex.Message}");
            }

            return elements;
        }, cancellationToken);
    }

    public async Task<List<UIElementInfo>> GetFlatUIElementListAsync(
        IntPtr windowHandle,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var elements = new List<UIElementInfo>();

            try
            {
                var rootElement = AutomationElement.FromHandle(windowHandle);
                if (rootElement != null)
                {
                    CollectElementsFlat(rootElement, elements, 0, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting UI elements: {ex.Message}");
            }

            return elements;
        }, cancellationToken);
    }

    private void CollectElementsFlat(
        AutomationElement element,
        List<UIElementInfo> elements,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (depth > MaxDepth)
            return;

        var info = CreateElementInfo(element, depth);
        elements.Add(info);

        try
        {
            var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            info.ChildCount = children.Count;

            foreach (AutomationElement child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CollectElementsFlat(child, elements, depth + 1, cancellationToken);
            }
        }
        catch (ElementNotAvailableException)
        {
            // Element was removed during enumeration
        }
    }

    private void PopulateChildren(
        AutomationElement element,
        UIElementInfo parentInfo,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (depth > MaxDepth)
            return;

        try
        {
            var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            parentInfo.ChildCount = children.Count;

            foreach (AutomationElement child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var childInfo = CreateElementInfo(child, depth);
                parentInfo.Children.Add(childInfo);

                PopulateChildren(child, childInfo, depth + 1, cancellationToken);
            }
        }
        catch (ElementNotAvailableException)
        {
            // Element was removed during enumeration
        }
    }

    private UIElementInfo CreateElementInfo(AutomationElement element, int depth)
    {
        var info = new UIElementInfo
        {
            Depth = depth
        };

        try
        {
            info.Name = element.Current.Name ?? string.Empty;
            info.AutomationId = element.Current.AutomationId ?? string.Empty;
            info.ControlType = element.Current.ControlType.ProgrammaticName.Replace("ControlType.", "");
            info.LocalizedControlType = element.Current.LocalizedControlType ?? string.Empty;
            info.ClassName = element.Current.ClassName ?? string.Empty;
            info.IsEnabled = element.Current.IsEnabled;
            info.IsOffscreen = element.Current.IsOffscreen;

            var rect = element.Current.BoundingRectangle;
            if (!rect.IsEmpty)
            {
                info.BoundingRectangle = new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            }

            // Try to get value if supported
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
            {
                var valuePattern = (ValuePattern)pattern;
                info.Value = valuePattern.Current.Value ?? string.Empty;
            }
        }
        catch (ElementNotAvailableException)
        {
            // Element became unavailable
        }

        return info;
    }
}
