using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Grimoire.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private string _title = "Grimoire";

    [ObservableProperty]
    private int _selectedNavIndex;

    private readonly GameLibraryViewModel _libraryVm;
    private readonly DownloadsViewModel _downloadsVm;
    private readonly SettingsViewModel _settingsVm;

    public MainWindowViewModel(GameLibraryViewModel libraryVm, DownloadsViewModel downloadsVm, SettingsViewModel settingsVm)
    {
        _libraryVm = libraryVm;
        _downloadsVm = downloadsVm;
        _settingsVm = settingsVm;
        _currentPage = libraryVm;
    }

    [RelayCommand]
    private void NavigateToLibrary()
    {
        CurrentPage = _libraryVm;
        SelectedNavIndex = 0;
    }

    [RelayCommand]
    private void NavigateToDownloads()
    {
        CurrentPage = _downloadsVm;
        SelectedNavIndex = 1;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPage = _settingsVm;
        SelectedNavIndex = 2;
    }

    [RelayCommand]
    private async Task RefreshLibrary()
    {
        await _libraryVm.LoadGamesCommand.ExecuteAsync(null);
    }

    public async Task LaunchFromProtocolAsync(int gameId)
    {
        NavigateToLibrary();
        await _libraryVm.LaunchGameByIdAsync(gameId);
    }
}
