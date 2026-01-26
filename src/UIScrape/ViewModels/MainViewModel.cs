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

    public MainViewModel()
    {
        _automationService = new UIAutomationService();
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
            var treeTask = _automationService.GetUIElementsAsync(SelectedProcess.MainWindowHandle, token);
            var flatTask = _automationService.GetFlatUIElementListAsync(SelectedProcess.MainWindowHandle, token);

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

    public void Cleanup()
    {
        _cancellationTokenSource?.Cancel();
        _refreshTimer.Stop();
    }
}
