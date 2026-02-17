using System.Runtime.InteropServices;
using System.Windows.Automation;
using UIScrape.Models;

namespace UIScrape.Services;

public class UIInteractionService
{
    // P/Invoke declarations for low-level input
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // Mouse event constants
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    // SendInput structures
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    // Windows messages
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CLOSE = 0x0010;

    /// <summary>
    /// Gets all available interaction methods for a given element.
    /// </summary>
    public List<InteractionMethod> GetAvailableInteractions(UIElementInfo elementInfo)
    {
        var methods = new List<InteractionMethod>();
        var element = elementInfo.AutomationElementRef;

        // === UI Automation Pattern-based interactions ===

        // Invoke (click buttons, links, menu items)
        methods.Add(new InteractionMethod
        {
            Name = "Invoke",
            Category = "Patterns",
            Description = "Invoke/activate the element (like clicking a button, link, or menu item)",
            IsAvailable = element != null && HasPattern(element, InvokePattern.Pattern),
            UnavailableReason = "Element does not support InvokePattern"
        });

        // Toggle (checkboxes, toggle buttons)
        methods.Add(new InteractionMethod
        {
            Name = "Toggle",
            Category = "Patterns",
            Description = "Toggle the element's state (checkbox, toggle button, etc.)",
            IsAvailable = element != null && HasPattern(element, TogglePattern.Pattern),
            UnavailableReason = "Element does not support TogglePattern"
        });

        // Set Value (text boxes, editable fields)
        methods.Add(new InteractionMethod
        {
            Name = "SetValue",
            Category = "Patterns",
            Description = "Set the text value of the element (text box, editable combo box, etc.)",
            RequiresParameter = true,
            ParameterHint = "Enter the text value to set",
            IsAvailable = element != null && HasPattern(element, ValuePattern.Pattern),
            UnavailableReason = "Element does not support ValuePattern"
        });

        // Get Value
        methods.Add(new InteractionMethod
        {
            Name = "GetValue",
            Category = "Patterns",
            Description = "Read the current value from the element",
            IsAvailable = element != null && HasPattern(element, ValuePattern.Pattern),
            UnavailableReason = "Element does not support ValuePattern"
        });

        // Selection Item - Select
        methods.Add(new InteractionMethod
        {
            Name = "Select",
            Category = "Patterns",
            Description = "Select this item (list item, radio button, tab, etc.)",
            IsAvailable = element != null && HasPattern(element, SelectionItemPattern.Pattern),
            UnavailableReason = "Element does not support SelectionItemPattern"
        });

        // Selection Item - Add to Selection
        methods.Add(new InteractionMethod
        {
            Name = "AddToSelection",
            Category = "Patterns",
            Description = "Add this item to the current selection (multi-select list)",
            IsAvailable = element != null && HasPattern(element, SelectionItemPattern.Pattern),
            UnavailableReason = "Element does not support SelectionItemPattern"
        });

        // Selection Item - Remove from Selection
        methods.Add(new InteractionMethod
        {
            Name = "RemoveFromSelection",
            Category = "Patterns",
            Description = "Remove this item from the current selection",
            IsAvailable = element != null && HasPattern(element, SelectionItemPattern.Pattern),
            UnavailableReason = "Element does not support SelectionItemPattern"
        });

        // Expand/Collapse
        methods.Add(new InteractionMethod
        {
            Name = "Expand",
            Category = "Patterns",
            Description = "Expand the element (tree node, combo box, menu, etc.)",
            IsAvailable = element != null && HasPattern(element, ExpandCollapsePattern.Pattern),
            UnavailableReason = "Element does not support ExpandCollapsePattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "Collapse",
            Category = "Patterns",
            Description = "Collapse the element (tree node, combo box, menu, etc.)",
            IsAvailable = element != null && HasPattern(element, ExpandCollapsePattern.Pattern),
            UnavailableReason = "Element does not support ExpandCollapsePattern"
        });

        // Range Value (sliders, spinners, progress bars)
        methods.Add(new InteractionMethod
        {
            Name = "SetRangeValue",
            Category = "Patterns",
            Description = "Set a numeric value within the element's range (slider, spinner, etc.)",
            RequiresParameter = true,
            ParameterHint = "Enter a numeric value",
            IsAvailable = element != null && HasPattern(element, RangeValuePattern.Pattern),
            UnavailableReason = "Element does not support RangeValuePattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "GetRangeInfo",
            Category = "Patterns",
            Description = "Get the current value, min, max, and step of a range control",
            IsAvailable = element != null && HasPattern(element, RangeValuePattern.Pattern),
            UnavailableReason = "Element does not support RangeValuePattern"
        });

        // Scroll
        methods.Add(new InteractionMethod
        {
            Name = "ScrollDown",
            Category = "Patterns",
            Description = "Scroll the container down by a large amount",
            IsAvailable = element != null && HasPattern(element, ScrollPattern.Pattern),
            UnavailableReason = "Element does not support ScrollPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "ScrollUp",
            Category = "Patterns",
            Description = "Scroll the container up by a large amount",
            IsAvailable = element != null && HasPattern(element, ScrollPattern.Pattern),
            UnavailableReason = "Element does not support ScrollPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "ScrollLeft",
            Category = "Patterns",
            Description = "Scroll the container left by a large amount",
            IsAvailable = element != null && HasPattern(element, ScrollPattern.Pattern),
            UnavailableReason = "Element does not support ScrollPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "ScrollRight",
            Category = "Patterns",
            Description = "Scroll the container right by a large amount",
            IsAvailable = element != null && HasPattern(element, ScrollPattern.Pattern),
            UnavailableReason = "Element does not support ScrollPattern"
        });

        // Scroll Item into View
        methods.Add(new InteractionMethod
        {
            Name = "ScrollIntoView",
            Category = "Patterns",
            Description = "Scroll this element into the visible area of its container",
            IsAvailable = element != null && HasPattern(element, ScrollItemPattern.Pattern),
            UnavailableReason = "Element does not support ScrollItemPattern"
        });

        // Window operations
        methods.Add(new InteractionMethod
        {
            Name = "WindowClose",
            Category = "Patterns",
            Description = "Close the window",
            IsAvailable = element != null && HasPattern(element, WindowPattern.Pattern),
            UnavailableReason = "Element does not support WindowPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "WindowMinimize",
            Category = "Patterns",
            Description = "Minimize the window",
            IsAvailable = element != null && HasPattern(element, WindowPattern.Pattern),
            UnavailableReason = "Element does not support WindowPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "WindowMaximize",
            Category = "Patterns",
            Description = "Maximize the window",
            IsAvailable = element != null && HasPattern(element, WindowPattern.Pattern),
            UnavailableReason = "Element does not support WindowPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "WindowRestore",
            Category = "Patterns",
            Description = "Restore the window to its normal size",
            IsAvailable = element != null && HasPattern(element, WindowPattern.Pattern),
            UnavailableReason = "Element does not support WindowPattern"
        });

        // Transform (move, resize)
        methods.Add(new InteractionMethod
        {
            Name = "TransformMove",
            Category = "Patterns",
            Description = "Move the element to a new position (x,y)",
            RequiresParameter = true,
            ParameterHint = "Enter coordinates as: x,y (e.g. 100,200)",
            IsAvailable = element != null && HasPattern(element, TransformPattern.Pattern),
            UnavailableReason = "Element does not support TransformPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "TransformResize",
            Category = "Patterns",
            Description = "Resize the element (width,height)",
            RequiresParameter = true,
            ParameterHint = "Enter dimensions as: width,height (e.g. 400,300)",
            IsAvailable = element != null && HasPattern(element, TransformPattern.Pattern),
            UnavailableReason = "Element does not support TransformPattern"
        });

        // Dock
        methods.Add(new InteractionMethod
        {
            Name = "SetDockPosition",
            Category = "Patterns",
            Description = "Set the dock position (Top, Bottom, Left, Right, Fill, None)",
            RequiresParameter = true,
            ParameterHint = "Enter: Top, Bottom, Left, Right, Fill, or None",
            IsAvailable = element != null && HasPattern(element, DockPattern.Pattern),
            UnavailableReason = "Element does not support DockPattern"
        });

        // Multiple View
        methods.Add(new InteractionMethod
        {
            Name = "GetSupportedViews",
            Category = "Patterns",
            Description = "List the views available for this element",
            IsAvailable = element != null && HasPattern(element, MultipleViewPattern.Pattern),
            UnavailableReason = "Element does not support MultipleViewPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "SetView",
            Category = "Patterns",
            Description = "Switch to a specific view by ID",
            RequiresParameter = true,
            ParameterHint = "Enter the view ID (use GetSupportedViews first)",
            IsAvailable = element != null && HasPattern(element, MultipleViewPattern.Pattern),
            UnavailableReason = "Element does not support MultipleViewPattern"
        });

        // Grid info
        methods.Add(new InteractionMethod
        {
            Name = "GetGridInfo",
            Category = "Patterns",
            Description = "Get grid dimensions (row count, column count)",
            IsAvailable = element != null && HasPattern(element, GridPattern.Pattern),
            UnavailableReason = "Element does not support GridPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "GetGridItem",
            Category = "Patterns",
            Description = "Get a grid cell at specified row,column",
            RequiresParameter = true,
            ParameterHint = "Enter as: row,column (e.g. 0,1)",
            IsAvailable = element != null && HasPattern(element, GridPattern.Pattern),
            UnavailableReason = "Element does not support GridPattern"
        });

        // Table info
        methods.Add(new InteractionMethod
        {
            Name = "GetTableHeaders",
            Category = "Patterns",
            Description = "Get the row and column headers of a table",
            IsAvailable = element != null && HasPattern(element, TablePattern.Pattern),
            UnavailableReason = "Element does not support TablePattern"
        });

        // Text Pattern
        methods.Add(new InteractionMethod
        {
            Name = "GetFullText",
            Category = "Patterns",
            Description = "Get the full text content of the element via TextPattern",
            IsAvailable = element != null && HasPattern(element, TextPattern.Pattern),
            UnavailableReason = "Element does not support TextPattern"
        });

        methods.Add(new InteractionMethod
        {
            Name = "GetSelectedText",
            Category = "Patterns",
            Description = "Get the currently selected text in the element",
            IsAvailable = element != null && HasPattern(element, TextPattern.Pattern),
            UnavailableReason = "Element does not support TextPattern"
        });

        // === Focus-based interaction ===

        methods.Add(new InteractionMethod
        {
            Name = "SetFocus",
            Category = "Focus",
            Description = "Set keyboard focus to this element via UI Automation",
            IsAvailable = element != null
        });

        // === Low-level mouse interactions (always available if bounding rect exists) ===

        bool hasBounds = elementInfo.BoundingRectangle.Width > 0 && elementInfo.BoundingRectangle.Height > 0;

        methods.Add(new InteractionMethod
        {
            Name = "LeftClick",
            Category = "Mouse",
            Description = "Send a left mouse click to the center of the element",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "DoubleClick",
            Category = "Mouse",
            Description = "Send a left mouse double-click to the center of the element",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "RightClick",
            Category = "Mouse",
            Description = "Send a right mouse click (context menu) to the center of the element",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "MiddleClick",
            Category = "Mouse",
            Description = "Send a middle mouse click to the center of the element",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "MouseHover",
            Category = "Mouse",
            Description = "Move the mouse cursor to the center of the element (hover)",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "ClickAtOffset",
            Category = "Mouse",
            Description = "Click at a specific offset from the element's top-left corner",
            RequiresParameter = true,
            ParameterHint = "Enter offset as: x,y (e.g. 5,5)",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "MouseWheelUp",
            Category = "Mouse",
            Description = "Send a mouse wheel scroll up at the element's position",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "MouseWheelDown",
            Category = "Mouse",
            Description = "Send a mouse wheel scroll down at the element's position",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "DragTo",
            Category = "Mouse",
            Description = "Click and drag from this element to a target position",
            RequiresParameter = true,
            ParameterHint = "Target as: x,y (e.g. 500,300)",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        // === Keyboard interactions ===

        methods.Add(new InteractionMethod
        {
            Name = "SendKeys",
            Category = "Keyboard",
            Description = "Send keystrokes to the focused element (focuses it first). Use text or special keys like {ENTER}, {TAB}, {ESCAPE}, {BACKSPACE}, {DELETE}, {UP}, {DOWN}, {LEFT}, {RIGHT}, {HOME}, {END}, {F1}-{F12}",
            RequiresParameter = true,
            ParameterHint = "Enter text or special keys (e.g. Hello{ENTER})",
            IsAvailable = true
        });

        methods.Add(new InteractionMethod
        {
            Name = "SendKeyCombo",
            Category = "Keyboard",
            Description = "Send a key combination (e.g. Ctrl+A, Ctrl+C, Alt+F4). Use Ctrl+, Alt+, Shift+, Win+ prefixes",
            RequiresParameter = true,
            ParameterHint = "Enter combo (e.g. Ctrl+A, Ctrl+Shift+S)",
            IsAvailable = true
        });

        // === Windows message-based interactions ===

        methods.Add(new InteractionMethod
        {
            Name = "SendWmClick",
            Category = "Messages",
            Description = "Send WM_LBUTTONDOWN + WM_LBUTTONUP messages to the element's window handle",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        methods.Add(new InteractionMethod
        {
            Name = "SendWmClose",
            Category = "Messages",
            Description = "Send WM_CLOSE message to the element's window handle",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        methods.Add(new InteractionMethod
        {
            Name = "SendWmChar",
            Category = "Messages",
            Description = "Send characters via WM_CHAR messages (bypasses keyboard layout issues)",
            RequiresParameter = true,
            ParameterHint = "Enter the text to send via WM_CHAR",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        methods.Add(new InteractionMethod
        {
            Name = "SendWmKeyDown",
            Category = "Messages",
            Description = "Send a WM_KEYDOWN message with a virtual key code",
            RequiresParameter = true,
            ParameterHint = "Enter virtual key code (e.g. 13 for Enter, 9 for Tab)",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        // === Utility ===

        methods.Add(new InteractionMethod
        {
            Name = "HighlightElement",
            Category = "Utility",
            Description = "Briefly flash/highlight the element's bounding rectangle on screen",
            IsAvailable = hasBounds,
            UnavailableReason = "Element has no valid bounding rectangle"
        });

        methods.Add(new InteractionMethod
        {
            Name = "BringWindowToFront",
            Category = "Utility",
            Description = "Bring the target application's window to the foreground",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        methods.Add(new InteractionMethod
        {
            Name = "GetAllProperties",
            Category = "Utility",
            Description = "Read and display all supported automation properties from the element",
            IsAvailable = element != null,
            UnavailableReason = "No element reference available"
        });

        return methods;
    }

    /// <summary>
    /// Execute an interaction method on the given element.
    /// </summary>
    public async Task<InteractionResult> ExecuteInteractionAsync(UIElementInfo elementInfo, string methodName, string? parameter = null)
    {
        return await Task.Run(() =>
        {
            var result = new InteractionResult
            {
                MethodName = methodName,
                Parameter = parameter,
                Timestamp = DateTime.Now
            };

            try
            {
                var element = elementInfo.AutomationElementRef;

                switch (methodName)
                {
                    // === Pattern-based ===
                    case "Invoke":
                        result = DoInvoke(element!, result);
                        break;
                    case "Toggle":
                        result = DoToggle(element!, result);
                        break;
                    case "SetValue":
                        result = DoSetValue(element!, parameter ?? "", result);
                        break;
                    case "GetValue":
                        result = DoGetValue(element!, result);
                        break;
                    case "Select":
                        result = DoSelect(element!, result);
                        break;
                    case "AddToSelection":
                        result = DoAddToSelection(element!, result);
                        break;
                    case "RemoveFromSelection":
                        result = DoRemoveFromSelection(element!, result);
                        break;
                    case "Expand":
                        result = DoExpand(element!, result);
                        break;
                    case "Collapse":
                        result = DoCollapse(element!, result);
                        break;
                    case "SetRangeValue":
                        result = DoSetRangeValue(element!, parameter ?? "", result);
                        break;
                    case "GetRangeInfo":
                        result = DoGetRangeInfo(element!, result);
                        break;
                    case "ScrollDown":
                        result = DoScroll(element!, ScrollAmount.LargeIncrement, ScrollAmount.NoAmount, result);
                        break;
                    case "ScrollUp":
                        result = DoScroll(element!, ScrollAmount.LargeDecrement, ScrollAmount.NoAmount, result);
                        break;
                    case "ScrollLeft":
                        result = DoScroll(element!, ScrollAmount.NoAmount, ScrollAmount.LargeDecrement, result);
                        break;
                    case "ScrollRight":
                        result = DoScroll(element!, ScrollAmount.NoAmount, ScrollAmount.LargeIncrement, result);
                        break;
                    case "ScrollIntoView":
                        result = DoScrollIntoView(element!, result);
                        break;
                    case "WindowClose":
                        result = DoWindowAction(element!, WindowVisualState.Normal, true, result);
                        break;
                    case "WindowMinimize":
                        result = DoWindowAction(element!, WindowVisualState.Minimized, false, result);
                        break;
                    case "WindowMaximize":
                        result = DoWindowAction(element!, WindowVisualState.Maximized, false, result);
                        break;
                    case "WindowRestore":
                        result = DoWindowAction(element!, WindowVisualState.Normal, false, result);
                        break;
                    case "TransformMove":
                        result = DoTransformMove(element!, parameter ?? "", result);
                        break;
                    case "TransformResize":
                        result = DoTransformResize(element!, parameter ?? "", result);
                        break;
                    case "SetDockPosition":
                        result = DoSetDockPosition(element!, parameter ?? "", result);
                        break;
                    case "GetSupportedViews":
                        result = DoGetSupportedViews(element!, result);
                        break;
                    case "SetView":
                        result = DoSetView(element!, parameter ?? "", result);
                        break;
                    case "GetGridInfo":
                        result = DoGetGridInfo(element!, result);
                        break;
                    case "GetGridItem":
                        result = DoGetGridItem(element!, parameter ?? "", result);
                        break;
                    case "GetTableHeaders":
                        result = DoGetTableHeaders(element!, result);
                        break;
                    case "GetFullText":
                        result = DoGetFullText(element!, result);
                        break;
                    case "GetSelectedText":
                        result = DoGetSelectedText(element!, result);
                        break;

                    // === Focus ===
                    case "SetFocus":
                        result = DoSetFocus(element!, result);
                        break;

                    // === Mouse ===
                    case "LeftClick":
                        result = DoMouseClick(elementInfo, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, result);
                        break;
                    case "DoubleClick":
                        result = DoDoubleClick(elementInfo, result);
                        break;
                    case "RightClick":
                        result = DoMouseClick(elementInfo, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, result);
                        break;
                    case "MiddleClick":
                        result = DoMouseClick(elementInfo, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, result);
                        break;
                    case "MouseHover":
                        result = DoMouseHover(elementInfo, result);
                        break;
                    case "ClickAtOffset":
                        result = DoClickAtOffset(elementInfo, parameter ?? "", result);
                        break;
                    case "MouseWheelUp":
                        result = DoMouseWheel(elementInfo, 120, result);
                        break;
                    case "MouseWheelDown":
                        result = DoMouseWheel(elementInfo, -120, result);
                        break;
                    case "DragTo":
                        result = DoDragTo(elementInfo, parameter ?? "", result);
                        break;

                    // === Keyboard ===
                    case "SendKeys":
                        result = DoSendKeys(elementInfo, parameter ?? "", result);
                        break;
                    case "SendKeyCombo":
                        result = DoSendKeyCombo(elementInfo, parameter ?? "", result);
                        break;

                    // === Windows messages ===
                    case "SendWmClick":
                        result = DoSendWmClick(element!, result);
                        break;
                    case "SendWmClose":
                        result = DoSendWmClose(element!, result);
                        break;
                    case "SendWmChar":
                        result = DoSendWmChar(element!, parameter ?? "", result);
                        break;
                    case "SendWmKeyDown":
                        result = DoSendWmKeyDown(element!, parameter ?? "", result);
                        break;

                    // === Utility ===
                    case "HighlightElement":
                        result = DoHighlight(elementInfo, result);
                        break;
                    case "BringWindowToFront":
                        result = DoBringToFront(element!, result);
                        break;
                    case "GetAllProperties":
                        result = DoGetAllProperties(element!, result);
                        break;

                    default:
                        result.Success = false;
                        result.Message = $"Unknown interaction method: {methodName}";
                        break;
                }
            }
            catch (ElementNotAvailableException)
            {
                result.Success = false;
                result.Message = "Element is no longer available (it may have been removed or the app changed). Try re-scanning.";
            }
            catch (InvalidOperationException ex)
            {
                result.Success = false;
                result.Message = $"Invalid operation: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.GetType().Name}: {ex.Message}";
            }

            return result;
        });
    }

    // ===== Pattern-based implementations =====

    private InteractionResult DoInvoke(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
        {
            ((InvokePattern)pattern).Invoke();
            result.Success = true;
            result.Message = "Invoke succeeded - element was activated";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "InvokePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoToggle(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern))
        {
            var togglePattern = (TogglePattern)pattern;
            var previousState = togglePattern.Current.ToggleState;
            togglePattern.Toggle();
            var newState = togglePattern.Current.ToggleState;
            result.Success = true;
            result.Message = $"Toggled from {previousState} to {newState}";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TogglePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoSetValue(AutomationElement element, string value, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            var valuePattern = (ValuePattern)pattern;
            if (valuePattern.Current.IsReadOnly)
            {
                result.Success = false;
                result.Message = "Element value is read-only";
            }
            else
            {
                valuePattern.SetValue(value);
                result.Success = true;
                result.Message = $"Value set to: \"{value}\"";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ValuePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetValue(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            var valuePattern = (ValuePattern)pattern;
            result.Success = true;
            result.Message = $"Current value: \"{valuePattern.Current.Value}\" (ReadOnly: {valuePattern.Current.IsReadOnly})";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ValuePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoSelect(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
        {
            ((SelectionItemPattern)pattern).Select();
            result.Success = true;
            result.Message = "Item selected";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "SelectionItemPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoAddToSelection(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
        {
            ((SelectionItemPattern)pattern).AddToSelection();
            result.Success = true;
            result.Message = "Item added to selection";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "SelectionItemPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoRemoveFromSelection(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
        {
            ((SelectionItemPattern)pattern).RemoveFromSelection();
            result.Success = true;
            result.Message = "Item removed from selection";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "SelectionItemPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoExpand(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
        {
            var ecp = (ExpandCollapsePattern)pattern;
            ecp.Expand();
            result.Success = true;
            result.Message = $"Element expanded (state: {ecp.Current.ExpandCollapseState})";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ExpandCollapsePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoCollapse(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
        {
            var ecp = (ExpandCollapsePattern)pattern;
            ecp.Collapse();
            result.Success = true;
            result.Message = $"Element collapsed (state: {ecp.Current.ExpandCollapseState})";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ExpandCollapsePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoSetRangeValue(AutomationElement element, string valueStr, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out var pattern))
        {
            var rangePattern = (RangeValuePattern)pattern;
            if (double.TryParse(valueStr, out double value))
            {
                var min = rangePattern.Current.Minimum;
                var max = rangePattern.Current.Maximum;
                rangePattern.SetValue(value);
                result.Success = true;
                result.Message = $"Range value set to {value} (range: {min} - {max})";
            }
            else
            {
                result.Success = false;
                result.Message = $"Could not parse \"{valueStr}\" as a number";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "RangeValuePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetRangeInfo(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out var pattern))
        {
            var rangePattern = (RangeValuePattern)pattern;
            result.Success = true;
            result.Message = $"Value: {rangePattern.Current.Value}, Min: {rangePattern.Current.Minimum}, Max: {rangePattern.Current.Maximum}, SmallChange: {rangePattern.Current.SmallChange}, LargeChange: {rangePattern.Current.LargeChange}, IsReadOnly: {rangePattern.Current.IsReadOnly}";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "RangeValuePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoScroll(AutomationElement element, ScrollAmount vertical, ScrollAmount horizontal, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ScrollPattern.Pattern, out var pattern))
        {
            var scrollPattern = (ScrollPattern)pattern;
            scrollPattern.Scroll(horizontal, vertical);
            result.Success = true;
            result.Message = $"Scrolled (H%: {scrollPattern.Current.HorizontalScrollPercent:F1}, V%: {scrollPattern.Current.VerticalScrollPercent:F1})";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ScrollPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoScrollIntoView(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var pattern))
        {
            ((ScrollItemPattern)pattern).ScrollIntoView();
            result.Success = true;
            result.Message = "Element scrolled into view";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "ScrollItemPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoWindowAction(AutomationElement element, WindowVisualState state, bool close, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            var windowPattern = (WindowPattern)pattern;
            if (close)
            {
                windowPattern.Close();
                result.Message = "Window close requested";
            }
            else
            {
                windowPattern.SetWindowVisualState(state);
                result.Message = $"Window state set to {state}";
            }
            result.Success = true;
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "WindowPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoTransformMove(AutomationElement element, string param, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TransformPattern.Pattern, out var pattern))
        {
            var transformPattern = (TransformPattern)pattern;
            var parts = param.Split(',');
            if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out double x) && double.TryParse(parts[1].Trim(), out double y))
            {
                transformPattern.Move(x, y);
                result.Success = true;
                result.Message = $"Element moved to ({x}, {y})";
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid format. Use: x,y (e.g. 100,200)";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TransformPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoTransformResize(AutomationElement element, string param, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TransformPattern.Pattern, out var pattern))
        {
            var transformPattern = (TransformPattern)pattern;
            var parts = param.Split(',');
            if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out double w) && double.TryParse(parts[1].Trim(), out double h))
            {
                transformPattern.Resize(w, h);
                result.Success = true;
                result.Message = $"Element resized to {w}x{h}";
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid format. Use: width,height (e.g. 400,300)";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TransformPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoSetDockPosition(AutomationElement element, string param, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(DockPattern.Pattern, out var pattern))
        {
            var dockPattern = (DockPattern)pattern;
            if (Enum.TryParse<DockPosition>(param.Trim(), true, out var position))
            {
                dockPattern.SetDockPosition(position);
                result.Success = true;
                result.Message = $"Dock position set to {position}";
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid dock position. Use: Top, Bottom, Left, Right, Fill, or None";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "DockPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetSupportedViews(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(MultipleViewPattern.Pattern, out var pattern))
        {
            var mvp = (MultipleViewPattern)pattern;
            var views = mvp.Current.GetSupportedViews();
            var viewNames = views.Select(v => $"ID={v}: {mvp.GetViewName(v)}");
            result.Success = true;
            result.Message = $"Current view: {mvp.Current.CurrentView}. Supported views: {string.Join(", ", viewNames)}";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "MultipleViewPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoSetView(AutomationElement element, string param, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(MultipleViewPattern.Pattern, out var pattern))
        {
            var mvp = (MultipleViewPattern)pattern;
            if (int.TryParse(param.Trim(), out int viewId))
            {
                mvp.SetCurrentView(viewId);
                result.Success = true;
                result.Message = $"View set to ID={viewId} ({mvp.GetViewName(viewId)})";
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid view ID. Enter a numeric ID.";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "MultipleViewPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetGridInfo(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(GridPattern.Pattern, out var pattern))
        {
            var gridPattern = (GridPattern)pattern;
            result.Success = true;
            result.Message = $"Grid: {gridPattern.Current.RowCount} rows x {gridPattern.Current.ColumnCount} columns";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "GridPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetGridItem(AutomationElement element, string param, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(GridPattern.Pattern, out var pattern))
        {
            var gridPattern = (GridPattern)pattern;
            var parts = param.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int row) && int.TryParse(parts[1].Trim(), out int col))
            {
                var item = gridPattern.GetItem(row, col);
                result.Success = true;
                result.Message = $"Cell [{row},{col}]: Name=\"{item.Current.Name}\", Type={item.Current.ControlType.ProgrammaticName}, AutomationId=\"{item.Current.AutomationId}\"";
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid format. Use: row,column (e.g. 0,1)";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "GridPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetTableHeaders(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TablePattern.Pattern, out var pattern))
        {
            var tablePattern = (TablePattern)pattern;
            var rowHeaders = tablePattern.Current.GetRowHeaders().Select(h => h.Current.Name);
            var colHeaders = tablePattern.Current.GetColumnHeaders().Select(h => h.Current.Name);
            result.Success = true;
            result.Message = $"Row headers: [{string.Join(", ", rowHeaders)}], Column headers: [{string.Join(", ", colHeaders)}]";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TablePattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetFullText(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
        {
            var textPattern = (TextPattern)pattern;
            var text = textPattern.DocumentRange.GetText(-1);
            result.Success = true;
            result.Message = text.Length > 500 ? $"Text ({text.Length} chars): \"{text[..500]}...\"" : $"Text: \"{text}\"";
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TextPattern not supported on this element";
        }
        return result;
    }

    private InteractionResult DoGetSelectedText(AutomationElement element, InteractionResult result)
    {
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
        {
            var textPattern = (TextPattern)pattern;
            var selections = textPattern.GetSelection();
            if (selections.Length > 0)
            {
                var selectedText = selections[0].GetText(-1);
                result.Success = true;
                result.Message = string.IsNullOrEmpty(selectedText) ? "No text is currently selected" : $"Selected text: \"{selectedText}\"";
            }
            else
            {
                result.Success = true;
                result.Message = "No text selection found";
            }
            result.Category = "Patterns";
        }
        else
        {
            result.Success = false;
            result.Message = "TextPattern not supported on this element";
        }
        return result;
    }

    // ===== Focus =====

    private InteractionResult DoSetFocus(AutomationElement element, InteractionResult result)
    {
        element.SetFocus();
        result.Success = true;
        result.Message = "Focus set to element";
        result.Category = "Focus";
        return result;
    }

    // ===== Mouse interactions =====

    private (int x, int y) GetElementCenter(UIElementInfo info)
    {
        var rect = info.BoundingRectangle;
        return ((int)(rect.X + rect.Width / 2), (int)(rect.Y + rect.Height / 2));
    }

    private InteractionResult DoMouseClick(UIElementInfo info, uint downFlag, uint upFlag, InteractionResult result)
    {
        var (x, y) = GetElementCenter(info);
        SetCursorPos(x, y);
        Thread.Sleep(50);
        mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
        result.Success = true;
        result.Message = $"Mouse click sent at ({x}, {y})";
        result.Category = "Mouse";
        return result;
    }

    private InteractionResult DoDoubleClick(UIElementInfo info, InteractionResult result)
    {
        var (x, y) = GetElementCenter(info);
        SetCursorPos(x, y);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        result.Success = true;
        result.Message = $"Double-click sent at ({x}, {y})";
        result.Category = "Mouse";
        return result;
    }

    private InteractionResult DoMouseHover(UIElementInfo info, InteractionResult result)
    {
        var (x, y) = GetElementCenter(info);
        SetCursorPos(x, y);
        result.Success = true;
        result.Message = $"Mouse moved to ({x}, {y}) - hovering over element";
        result.Category = "Mouse";
        return result;
    }

    private InteractionResult DoClickAtOffset(UIElementInfo info, string param, InteractionResult result)
    {
        var parts = param.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int offX) && int.TryParse(parts[1].Trim(), out int offY))
        {
            var x = (int)info.BoundingRectangle.X + offX;
            var y = (int)info.BoundingRectangle.Y + offY;
            SetCursorPos(x, y);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            result.Success = true;
            result.Message = $"Clicked at offset ({offX},{offY}) -> screen ({x},{y})";
        }
        else
        {
            result.Success = false;
            result.Message = "Invalid format. Use: x,y (e.g. 5,5)";
        }
        result.Category = "Mouse";
        return result;
    }

    private InteractionResult DoMouseWheel(UIElementInfo info, int delta, InteractionResult result)
    {
        var (x, y) = GetElementCenter(info);
        SetCursorPos(x, y);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
        result.Success = true;
        result.Message = $"Mouse wheel {(delta > 0 ? "up" : "down")} at ({x}, {y})";
        result.Category = "Mouse";
        return result;
    }

    private InteractionResult DoDragTo(UIElementInfo info, string param, InteractionResult result)
    {
        var parts = param.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int targetX) && int.TryParse(parts[1].Trim(), out int targetY))
        {
            var (startX, startY) = GetElementCenter(info);
            SetCursorPos(startX, startY);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);

            // Move in small steps for smooth drag
            int steps = 20;
            for (int i = 1; i <= steps; i++)
            {
                int cx = startX + (targetX - startX) * i / steps;
                int cy = startY + (targetY - startY) * i / steps;
                SetCursorPos(cx, cy);
                Thread.Sleep(15);
            }

            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            result.Success = true;
            result.Message = $"Dragged from ({startX},{startY}) to ({targetX},{targetY})";
        }
        else
        {
            result.Success = false;
            result.Message = "Invalid format. Use: x,y (e.g. 500,300)";
        }
        result.Category = "Mouse";
        return result;
    }

    // ===== Keyboard interactions =====

    private static readonly Dictionary<string, ushort> SpecialKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ENTER", 0x0D }, { "RETURN", 0x0D },
        { "TAB", 0x09 },
        { "ESCAPE", 0x1B }, { "ESC", 0x1B },
        { "BACKSPACE", 0x08 }, { "BS", 0x08 },
        { "DELETE", 0x2E }, { "DEL", 0x2E },
        { "INSERT", 0x2D }, { "INS", 0x2D },
        { "UP", 0x26 },
        { "DOWN", 0x28 },
        { "LEFT", 0x25 },
        { "RIGHT", 0x27 },
        { "HOME", 0x24 },
        { "END", 0x23 },
        { "PGUP", 0x21 }, { "PAGEUP", 0x21 },
        { "PGDN", 0x22 }, { "PAGEDOWN", 0x22 },
        { "SPACE", 0x20 },
        { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
        { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
        { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
        { "PRINTSCREEN", 0x2C }, { "PRTSC", 0x2C },
        { "SCROLLLOCK", 0x91 },
        { "PAUSE", 0x13 },
        { "NUMLOCK", 0x90 },
        { "CAPSLOCK", 0x14 },
        { "APPS", 0x5D },
    };

    private InteractionResult DoSendKeys(UIElementInfo info, string keys, InteractionResult result)
    {
        try
        {
            // Focus the element first
            info.AutomationElementRef?.SetFocus();
            Thread.Sleep(100);

            var inputs = new List<INPUT>();

            int i = 0;
            while (i < keys.Length)
            {
                if (keys[i] == '{')
                {
                    int end = keys.IndexOf('}', i + 1);
                    if (end > i)
                    {
                        var keyName = keys[(i + 1)..end];
                        if (SpecialKeys.TryGetValue(keyName, out ushort vk))
                        {
                            inputs.Add(MakeKeyInput(vk, false));
                            inputs.Add(MakeKeyInput(vk, true));
                        }
                        i = end + 1;
                        continue;
                    }
                }

                // Send as unicode character
                inputs.Add(MakeUnicodeInput(keys[i], false));
                inputs.Add(MakeUnicodeInput(keys[i], true));
                i++;
            }

            if (inputs.Count > 0)
            {
                var inputArray = inputs.ToArray();
                SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<INPUT>());
            }

            result.Success = true;
            result.Message = $"Sent {keys.Length} character(s) / key(s) to element";
            result.Category = "Keyboard";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed to send keys: {ex.Message}";
        }
        return result;
    }

    private InteractionResult DoSendKeyCombo(UIElementInfo info, string combo, InteractionResult result)
    {
        try
        {
            info.AutomationElementRef?.SetFocus();
            Thread.Sleep(100);

            var parts = combo.Split('+');
            var modifiers = new List<ushort>();
            ushort mainKey = 0;

            foreach (var part in parts)
            {
                var p = part.Trim();
                switch (p.ToUpperInvariant())
                {
                    case "CTRL": case "CONTROL":
                        modifiers.Add(0xA2); // VK_LCONTROL
                        break;
                    case "ALT":
                        modifiers.Add(0xA4); // VK_LMENU
                        break;
                    case "SHIFT":
                        modifiers.Add(0xA0); // VK_LSHIFT
                        break;
                    case "WIN": case "WINDOWS":
                        modifiers.Add(0x5B); // VK_LWIN
                        break;
                    default:
                        if (SpecialKeys.TryGetValue(p.ToUpperInvariant(), out ushort specialVk))
                        {
                            mainKey = specialVk;
                        }
                        else if (p.Length == 1)
                        {
                            mainKey = (ushort)char.ToUpperInvariant(p[0]);
                        }
                        break;
                }
            }

            var inputs = new List<INPUT>();

            // Press modifiers
            foreach (var mod in modifiers)
                inputs.Add(MakeKeyInput(mod, false));

            // Press and release main key
            if (mainKey != 0)
            {
                inputs.Add(MakeKeyInput(mainKey, false));
                inputs.Add(MakeKeyInput(mainKey, true));
            }

            // Release modifiers in reverse
            for (int j = modifiers.Count - 1; j >= 0; j--)
                inputs.Add(MakeKeyInput(modifiers[j], true));

            if (inputs.Count > 0)
            {
                var inputArray = inputs.ToArray();
                SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<INPUT>());
            }

            result.Success = true;
            result.Message = $"Key combination '{combo}' sent";
            result.Category = "Keyboard";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed to send key combo: {ex.Message}";
        }
        return result;
    }

    private static INPUT MakeKeyInput(ushort vk, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            }
        };
    }

    private static INPUT MakeUnicodeInput(char c, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0)
                }
            }
        };
    }

    // ===== Windows message-based interactions =====

    private InteractionResult DoSendWmClick(AutomationElement element, InteractionResult result)
    {
        try
        {
            var hwnd = new IntPtr(element.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero)
            {
                PostMessage(hwnd, WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(50);
                PostMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
                result.Success = true;
                result.Message = $"WM_LBUTTONDOWN/UP sent to window handle 0x{hwnd:X}";
            }
            else
            {
                result.Success = false;
                result.Message = "Element has no native window handle (NativeWindowHandle = 0). This element may be rendered within a parent window.";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed: {ex.Message}";
        }
        result.Category = "Messages";
        return result;
    }

    private InteractionResult DoSendWmClose(AutomationElement element, InteractionResult result)
    {
        try
        {
            var hwnd = new IntPtr(element.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero)
            {
                PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                result.Success = true;
                result.Message = $"WM_CLOSE sent to window handle 0x{hwnd:X}";
            }
            else
            {
                result.Success = false;
                result.Message = "Element has no native window handle";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed: {ex.Message}";
        }
        result.Category = "Messages";
        return result;
    }

    private InteractionResult DoSendWmChar(AutomationElement element, string text, InteractionResult result)
    {
        try
        {
            var hwnd = new IntPtr(element.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero)
            {
                foreach (char c in text)
                {
                    PostMessage(hwnd, WM_CHAR, (IntPtr)c, IntPtr.Zero);
                    Thread.Sleep(10);
                }
                result.Success = true;
                result.Message = $"Sent {text.Length} WM_CHAR messages to 0x{hwnd:X}";
            }
            else
            {
                result.Success = false;
                result.Message = "Element has no native window handle";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed: {ex.Message}";
        }
        result.Category = "Messages";
        return result;
    }

    private InteractionResult DoSendWmKeyDown(AutomationElement element, string vkStr, InteractionResult result)
    {
        try
        {
            var hwnd = new IntPtr(element.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero)
            {
                if (int.TryParse(vkStr.Trim(), out int vk))
                {
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)vk, IntPtr.Zero);
                    Thread.Sleep(50);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)vk, IntPtr.Zero);
                    result.Success = true;
                    result.Message = $"WM_KEYDOWN/UP with VK=0x{vk:X2} ({vk}) sent to 0x{hwnd:X}";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Could not parse \"{vkStr}\" as an integer key code";
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Element has no native window handle";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed: {ex.Message}";
        }
        result.Category = "Messages";
        return result;
    }

    // ===== Utility =====

    private InteractionResult DoHighlight(UIElementInfo info, InteractionResult result)
    {
        // We'll signal the UI to draw a highlight overlay; for now, just report the rect
        result.Success = true;
        result.Message = $"Element bounds: X={info.BoundingRectangle.X}, Y={info.BoundingRectangle.Y}, W={info.BoundingRectangle.Width}, H={info.BoundingRectangle.Height}";
        result.Category = "Utility";

        // Flash cursor to element location to give visual feedback
        var (x, y) = GetElementCenter(info);
        SetCursorPos(x, y);

        return result;
    }

    private InteractionResult DoBringToFront(AutomationElement element, InteractionResult result)
    {
        try
        {
            var hwnd = new IntPtr(element.Current.NativeWindowHandle);
            if (hwnd == IntPtr.Zero)
            {
                // Walk up to find a parent with a window handle
                var walker = TreeWalker.ControlViewWalker;
                var current = element;
                while (current != null && new IntPtr(current.Current.NativeWindowHandle) == IntPtr.Zero)
                {
                    current = walker.GetParent(current);
                }
                if (current != null)
                    hwnd = new IntPtr(current.Current.NativeWindowHandle);
            }

            if (hwnd != IntPtr.Zero)
            {
                // Attach to the thread of the target window so SetForegroundWindow works
                GetWindowThreadProcessId(hwnd, out _);
                SetForegroundWindow(hwnd);
                result.Success = true;
                result.Message = $"Window brought to foreground (handle: 0x{hwnd:X})";
            }
            else
            {
                result.Success = false;
                result.Message = "Could not find a window handle for this element or its parents";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed: {ex.Message}";
        }
        result.Category = "Utility";
        return result;
    }

    private InteractionResult DoGetAllProperties(AutomationElement element, InteractionResult result)
    {
        try
        {
            var props = new List<string>
            {
                $"Name: {element.Current.Name}",
                $"AutomationId: {element.Current.AutomationId}",
                $"ControlType: {element.Current.ControlType.ProgrammaticName}",
                $"LocalizedControlType: {element.Current.LocalizedControlType}",
                $"ClassName: {element.Current.ClassName}",
                $"NativeWindowHandle: 0x{element.Current.NativeWindowHandle:X}",
                $"ProcessId: {element.Current.ProcessId}",
                $"IsEnabled: {element.Current.IsEnabled}",
                $"IsOffscreen: {element.Current.IsOffscreen}",
                $"IsKeyboardFocusable: {element.Current.IsKeyboardFocusable}",
                $"HasKeyboardFocus: {element.Current.HasKeyboardFocus}",
                $"IsContentElement: {element.Current.IsContentElement}",
                $"IsControlElement: {element.Current.IsControlElement}",
                $"IsPassword: {element.Current.IsPassword}",
                $"BoundingRectangle: {element.Current.BoundingRectangle}",
                $"ItemType: {element.Current.ItemType}",
                $"ItemStatus: {element.Current.ItemStatus}",
                $"FrameworkId: {element.Current.FrameworkId}",
                $"LabeledBy: {(element.Current.LabeledBy != null ? element.Current.LabeledBy.Current.Name : "(none)")}",
                $"AcceleratorKey: {element.Current.AcceleratorKey}",
                $"AccessKey: {element.Current.AccessKey}",
                $"HelpText: {element.Current.HelpText}"
            };

            result.Success = true;
            result.Message = string.Join("\n", props);
            result.Category = "Utility";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed to read properties: {ex.Message}";
        }
        return result;
    }

    // ===== Helpers =====

    private static bool HasPattern(AutomationElement element, AutomationPattern pattern)
    {
        try
        {
            return element.TryGetCurrentPattern(pattern, out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects all supported patterns on an AutomationElement and returns their names.
    /// </summary>
    public static List<string> DetectSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();

        var patternChecks = new (AutomationPattern pattern, string name)[]
        {
            (InvokePattern.Pattern, "Invoke"),
            (TogglePattern.Pattern, "Toggle"),
            (ValuePattern.Pattern, "Value"),
            (SelectionItemPattern.Pattern, "SelectionItem"),
            (SelectionPattern.Pattern, "Selection"),
            (ExpandCollapsePattern.Pattern, "ExpandCollapse"),
            (RangeValuePattern.Pattern, "RangeValue"),
            (ScrollPattern.Pattern, "Scroll"),
            (ScrollItemPattern.Pattern, "ScrollItem"),
            (WindowPattern.Pattern, "Window"),
            (TransformPattern.Pattern, "Transform"),
            (DockPattern.Pattern, "Dock"),
            (GridPattern.Pattern, "Grid"),
            (GridItemPattern.Pattern, "GridItem"),
            (TablePattern.Pattern, "Table"),
            (TableItemPattern.Pattern, "TableItem"),
            (TextPattern.Pattern, "Text"),
            (MultipleViewPattern.Pattern, "MultipleView"),
        };

        foreach (var (pat, name) in patternChecks)
        {
            try
            {
                if (element.TryGetCurrentPattern(pat, out _))
                    patterns.Add(name);
            }
            catch
            {
                // Element may have become unavailable
            }
        }

        return patterns;
    }
}
