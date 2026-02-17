using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UIScrape.Models;
using UIScrape.Services;

namespace UIScrape.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UIAutomationService _automationService;
    private readonly UIInteractionService _interactionService;
    private readonly DispatcherTimer _refreshTimer;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<ProcessInfo> _processes = new();

    [ObservableProperty]
    private ProcessInfo? _selectedProcess;

    [ObservableProperty]
    private ObservableCollection<UIElementInfo> _uiElements = new();

    [ObservableProperty]
    private List<UIElementInfo> _flatElementList = new();

    [ObservableProperty]
    private UIElementInfo? _selectedElement;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    [ObservableProperty]
    private int _refreshIntervalSeconds = 2;

    [ObservableProperty]
    private string _statusMessage = "Select an application to inspect";

    [ObservableProperty]
    private int _totalElementCount;

    [ObservableProperty]
    private bool _showTreeView = true;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private List<UIElementInfo> _filteredFlatList = new();

    // === Interaction testing state ===

    [ObservableProperty]
    private List<InteractionMethod> _availableInteractions = new();

    [ObservableProperty]
    private InteractionMethod? _selectedInteraction;

    [ObservableProperty]
    private string _interactionParameter = string.Empty;

    [ObservableProperty]
    private ObservableCollection<InteractionResult> _interactionResults = new();

    [ObservableProperty]
    private bool _isInteracting;

    [ObservableProperty]
    private string _interactionFilterCategory = "All";

    [ObservableProperty]
    private List<InteractionMethod> _filteredInteractions = new();

    public List<string> InteractionCategories { get; } = new()
    {
        "All", "Patterns", "Focus", "Mouse", "Keyboard", "Messages", "Utility"
    };

    public MainViewModel()
    {
        _automationService = new UIAutomationService();
        _interactionService = new UIInteractionService();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(RefreshIntervalSeconds)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshElementsAsync();

        LoadProcesses();
    }

    partial void OnSelectedProcessChanged(ProcessInfo? value)
    {
        if (value != null)
        {
            _ = RefreshElementsAsync();
        }
        else
        {
            UiElements.Clear();
            FlatElementList.Clear();
            FilteredFlatList.Clear();
            TotalElementCount = 0;
            StatusMessage = "Select an application to inspect";
        }
    }

    partial void OnSelectedElementChanged(UIElementInfo? value)
    {
        // Update available interactions when element selection changes
        if (value != null)
        {
            AvailableInteractions = _interactionService.GetAvailableInteractions(value);
            ApplyInteractionFilter();
        }
        else
        {
            AvailableInteractions = new List<InteractionMethod>();
            FilteredInteractions = new List<InteractionMethod>();
        }
        SelectedInteraction = null;
        InteractionParameter = string.Empty;
    }

    partial void OnSelectedInteractionChanged(InteractionMethod? value)
    {
        InteractionParameter = string.Empty;
    }

    partial void OnInteractionFilterCategoryChanged(string value)
    {
        ApplyInteractionFilter();
    }

    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            _refreshTimer.Start();
            StatusMessage = $"Auto-refresh enabled ({RefreshIntervalSeconds}s interval)";
        }
        else
        {
            _refreshTimer.Stop();
            StatusMessage = "Auto-refresh disabled";
        }
    }

    partial void OnRefreshIntervalSecondsChanged(int value)
    {
        _refreshTimer.Interval = TimeSpan.FromSeconds(value);
        if (IsAutoRefreshEnabled)
        {
            StatusMessage = $"Auto-refresh interval changed to {value}s";
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void LoadProcesses()
    {
        var currentSelection = SelectedProcess?.ProcessId;

        Processes.Clear();
        foreach (var process in _automationService.GetWindowedProcesses())
        {
            Processes.Add(process);
        }

        // Try to restore selection
        if (currentSelection.HasValue)
        {
            SelectedProcess = Processes.FirstOrDefault(p => p.ProcessId == currentSelection.Value);
        }

        StatusMessage = $"Found {Processes.Count} windowed applications";
    }

    [RelayCommand]
    private async Task RefreshElementsAsync()
    {
        if (SelectedProcess == null || IsLoading)
            return;

        // Cancel any ongoing operation
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        IsLoading = true;
        StatusMessage = "Scanning UI elements...";

        try
        {
            // Get both tree and flat list
            var processName = SelectedProcess.ProcessName;
            var treeTask = _automationService.GetUIElementsAsync(SelectedProcess.MainWindowHandle, processName, token);
            var flatTask = _automationService.GetFlatUIElementListAsync(SelectedProcess.MainWindowHandle, processName, token);

            await Task.WhenAll(treeTask, flatTask);

            if (!token.IsCancellationRequested)
            {
                UiElements = await treeTask;
                FlatElementList = await flatTask;
                TotalElementCount = FlatElementList.Count;
                ApplyFilter();
                StatusMessage = $"Found {TotalElementCount} UI elements";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleView()
    {
        ShowTreeView = !ShowTreeView;
    }

    [RelayCommand]
    private async Task ExecuteInteractionAsync()
    {
        if (SelectedElement == null || SelectedInteraction == null || IsInteracting)
            return;

        if (!SelectedInteraction.IsAvailable)
        {
            StatusMessage = $"Interaction '{SelectedInteraction.Name}' is not available for this element";
            return;
        }

        if (SelectedInteraction.RequiresParameter && string.IsNullOrWhiteSpace(InteractionParameter))
        {
            StatusMessage = $"Parameter required: {SelectedInteraction.ParameterHint}";
            return;
        }

        IsInteracting = true;
        StatusMessage = $"Executing: {SelectedInteraction.Name}...";

        try
        {
            var result = await _interactionService.ExecuteInteractionAsync(
                SelectedElement,
                SelectedInteraction.Name,
                SelectedInteraction.RequiresParameter ? InteractionParameter : null);

            InteractionResults.Insert(0, result);

            StatusMessage = result.Success
                ? $"Success: {SelectedInteraction.Name}"
                : $"Failed: {SelectedInteraction.Name} - {result.Message}";
        }
        catch (Exception ex)
        {
            var result = new InteractionResult
            {
                MethodName = SelectedInteraction.Name,
                Parameter = InteractionParameter,
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
            InteractionResults.Insert(0, result);
            StatusMessage = $"Error executing {SelectedInteraction.Name}: {ex.Message}";
        }
        finally
        {
            IsInteracting = false;
        }
    }

    [RelayCommand]
    private async Task TryAllAvailableAsync()
    {
        if (SelectedElement == null || IsInteracting)
            return;

        IsInteracting = true;
        StatusMessage = "Running all available non-destructive interactions...";

        try
        {
            // Only try read-only / non-destructive methods
            var safeReadMethods = new HashSet<string>
            {
                "GetValue", "GetRangeInfo", "GetSupportedViews", "GetGridInfo",
                "GetTableHeaders", "GetFullText", "GetSelectedText",
                "SetFocus", "MouseHover", "ScrollIntoView",
                "HighlightElement", "GetAllProperties"
            };

            var available = AvailableInteractions.Where(m => m.IsAvailable && safeReadMethods.Contains(m.Name)).ToList();

            foreach (var method in available)
            {
                var result = await _interactionService.ExecuteInteractionAsync(
                    SelectedElement, method.Name);
                InteractionResults.Insert(0, result);
            }

            StatusMessage = $"Completed {available.Count} safe interaction tests";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during batch test: {ex.Message}";
        }
        finally
        {
            IsInteracting = false;
        }
    }

    [RelayCommand]
    private void ClearInteractionResults()
    {
        InteractionResults.Clear();
        StatusMessage = "Interaction log cleared";
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            FilteredFlatList = FlatElementList.ToList();
        }
        else
        {
            var filter = FilterText.ToLowerInvariant();
            FilteredFlatList = FlatElementList
                .Where(e =>
                    e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.AutomationId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.ControlType.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.ClassName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.Value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void ApplyInteractionFilter()
    {
        if (InteractionFilterCategory == "All")
        {
            FilteredInteractions = AvailableInteractions.ToList();
        }
        else
        {
            FilteredInteractions = AvailableInteractions
                .Where(m => m.Category == InteractionFilterCategory)
                .ToList();
        }
    }

    public void Cleanup()
    {
        _cancellationTokenSource?.Cancel();
        _refreshTimer.Stop();
    }
}
