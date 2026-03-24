using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Desktop.Services;

namespace Grimoire.Desktop.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    private readonly IDownloadManager _downloadManager;

    public ObservableCollection<DownloadItem> Downloads => _downloadManager.Downloads;

    [ObservableProperty]
    private int _activeCount;

    public DownloadsViewModel(IDownloadManager downloadManager)
    {
        _downloadManager = downloadManager;
    }

    [RelayCommand]
    private void CancelDownload(DownloadItem item)
    {
        _downloadManager.Cancel(item.Id);
    }
}
