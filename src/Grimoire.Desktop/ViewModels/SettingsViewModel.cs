using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Desktop.Services;

namespace Grimoire.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private string _serverUrl = "http://localhost:5038";

    [ObservableProperty]
    private string _installDirectory = "";

    [ObservableProperty]
    private string? _statusMessage;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        ServerUrl = await _settings.GetServerUrlAsync();
        InstallDirectory = await _settings.GetInstallDirectoryAsync();
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        await _settings.SetServerUrlAsync(ServerUrl);
        await _settings.SetAsync(SettingsService.InstallDirectoryKey, InstallDirectory);
        StatusMessage = "Settings saved.";
    }
}
