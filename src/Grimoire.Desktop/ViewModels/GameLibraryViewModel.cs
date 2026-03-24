using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Desktop.Services;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Desktop.ViewModels;

public partial class GameLibraryViewModel : ViewModelBase
{
    private readonly IGrimoireApi _api;
    private readonly ILaunchService _launchService;

    [ObservableProperty]
    private ObservableCollection<GameListDto> _games = [];

    [ObservableProperty]
    private GameListDto? _selectedGame;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private PlatformType? _selectedPlatform;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private GameDetailDto? _gameDetail;

    [ObservableProperty]
    private bool _showDetail;

    [ObservableProperty]
    private bool _isLaunching;

    [ObservableProperty]
    private string? _launchStatus;

    [ObservableProperty]
    private double _launchProgress;

    [ObservableProperty]
    private bool _showLaunchProgress;

    public GameLibraryViewModel(IGrimoireApi api, ILaunchService launchService)
    {
        _api = api;
        _launchService = launchService;
    }

    [RelayCommand]
    private async Task LoadGames()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var result = await _api.GetGamesAsync(SelectedPlatform, search);

            Games = new ObservableCollection<GameListDto>(result);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Cannot connect to server: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewGameDetail(GameListDto game)
    {
        try
        {
            GameDetail = await _api.GetGameDetailAsync(game.Id);
            ShowDetail = true;
            LaunchStatus = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading game details: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        ShowDetail = false;
        GameDetail = null;
        LaunchStatus = null;
    }

    [RelayCommand]
    private async Task LaunchGame()
    {
        if (GameDetail is null) return;

        try
        {
            IsLaunching = true;
            ShowLaunchProgress = true;
            LaunchProgress = 0;
            LaunchStatus = "Preparing...";

            var stepWeights = new Dictionary<LaunchStep, double>
            {
                [LaunchStep.ResolvingGame] = 5,
                [LaunchStep.CheckingEmulator] = 10,
                [LaunchStep.DownloadingEmulator] = 30,
                [LaunchStep.InstallingEmulator] = 40,
                [LaunchStep.DownloadingGame] = 60,
                [LaunchStep.ValidatingRequirements] = 75,
                [LaunchStep.Launching] = 90,
                [LaunchStep.Complete] = 100,
            };

            var progress = new Progress<LaunchProgress>(p =>
            {
                LaunchStatus = p.Message;

                // Use actual download percentage when available, otherwise use step weights
                if (p.ProgressPercent.HasValue && p.Step is LaunchStep.DownloadingGame or LaunchStep.DownloadingEmulator)
                    LaunchProgress = p.ProgressPercent.Value;
                else if (stepWeights.TryGetValue(p.Step, out var weight))
                    LaunchProgress = weight;

                if (p.Step == LaunchStep.Complete)
                    ShowLaunchProgress = false;
            });

            await _launchService.LaunchGameAsync(GameDetail.Id, progress);
        }
        catch (Exception ex)
        {
            LaunchStatus = $"Failed: {ex.Message}";
            ShowLaunchProgress = false;
        }
        finally
        {
            IsLaunching = false;
        }
    }

    /// <summary>
    /// Called from protocol activation (grimoire://launch/{gameId})
    /// </summary>
    public async Task LaunchGameByIdAsync(int gameId)
    {
        try
        {
            IsLaunching = true;
            LaunchStatus = "Launching from protocol...";

            var progress = new Progress<LaunchProgress>(p =>
            {
                LaunchStatus = p.Message;
            });

            // Show the game detail while launching
            GameDetail = await _api.GetGameDetailAsync(gameId);
            ShowDetail = true;

            await _launchService.LaunchGameAsync(gameId, progress);
        }
        catch (Exception ex)
        {
            LaunchStatus = $"Failed: {ex.Message}";
        }
        finally
        {
            IsLaunching = false;
        }
    }

    [RelayCommand]
    private async Task FilterPlatform(string? platformStr)
    {
        SelectedPlatform = platformStr is null ? null : Enum.Parse<PlatformType>(platformStr);
        await LoadGames();
    }
}
