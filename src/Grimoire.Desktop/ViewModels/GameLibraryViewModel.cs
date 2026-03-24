using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Desktop.ViewModels;

public partial class GameLibraryViewModel : ViewModelBase
{
    private readonly IGrimoireApi _api;

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

    public GameLibraryViewModel(IGrimoireApi api)
    {
        _api = api;
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
    }

    [RelayCommand]
    private async Task FilterPlatform(string? platformStr)
    {
        SelectedPlatform = platformStr is null ? null : Enum.Parse<PlatformType>(platformStr);
        await LoadGames();
    }
}
