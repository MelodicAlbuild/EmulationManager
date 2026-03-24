using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Desktop.Services;

public class DownloadItem : INotifyPropertyChanged
{
    public string Id { get; }
    public string Name { get; }
    public DownloadableType Type { get; }
    public int ServerId { get; }
    public string DestinationPath { get; }

    private long _bytesDownloaded;
    public long BytesDownloaded
    {
        get => _bytesDownloaded;
        set { _bytesDownloaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); OnPropertyChanged(nameof(ProgressText)); }
    }

    private long _totalBytes;
    public long TotalBytes
    {
        get => _totalBytes;
        set { _totalBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); OnPropertyChanged(nameof(ProgressText)); }
    }

    private DownloadStatus _status = DownloadStatus.Queued;
    public DownloadStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    private string? _error;
    public string? Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(); }
    }

    public double ProgressPercent => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100.0 : 0.0;

    public string ProgressText
    {
        get
        {
            if (TotalBytes <= 0) return "";
            return $"{FormatBytes(BytesDownloaded)} / {FormatBytes(TotalBytes)}";
        }
    }

    public string StatusText => Status switch
    {
        DownloadStatus.Queued => "Queued",
        DownloadStatus.Downloading => "Downloading...",
        DownloadStatus.Paused => "Paused",
        DownloadStatus.Completed => "Complete",
        DownloadStatus.Failed => "Failed",
        _ => Status.ToString()
    };

    public DownloadItem(string id, string name, DownloadableType type, int serverId, string destinationPath)
    {
        Id = id;
        Name = name;
        Type = type;
        ServerId = serverId;
        DestinationPath = destinationPath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_000_000_000 => $"{bytes / 1_000_000_000.0:F1} GB",
        >= 1_000_000 => $"{bytes / 1_000_000.0:F1} MB",
        >= 1_000 => $"{bytes / 1_000.0:F1} KB",
        _ => $"{bytes} B"
    };
}

public interface IDownloadManager
{
    ObservableCollection<DownloadItem> Downloads { get; }
    Task<DownloadItem> EnqueueAsync(string name, DownloadableType type, int serverId, string destinationPath);
    void Cancel(string downloadId);
    event Action<DownloadItem>? DownloadCompleted;
}

public class DownloadManager : IDownloadManager, IDisposable
{
    private readonly IGrimoireApi _api;
    private readonly Channel<DownloadItem> _channel;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _disposeCts = new();

    private const int MaxConcurrentDownloads = 2;
    private const int BufferSize = 81920; // 80KB buffer

    public ObservableCollection<DownloadItem> Downloads { get; } = [];
    public event Action<DownloadItem>? DownloadCompleted;

    public DownloadManager(IGrimoireApi api)
    {
        _api = api;
        _channel = Channel.CreateBounded<DownloadItem>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        _processingTask = ProcessDownloadsAsync(_disposeCts.Token);
    }

    public async Task<DownloadItem> EnqueueAsync(string name, DownloadableType type, int serverId, string destinationPath)
    {
        var item = new DownloadItem(
            Guid.NewGuid().ToString("N"),
            name,
            type,
            serverId,
            destinationPath
        );

        Downloads.Add(item);
        await _channel.Writer.WriteAsync(item);
        return item;
    }

    public void Cancel(string downloadId)
    {
        if (_cancellations.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
        }
    }

    private async Task ProcessDownloadsAsync(CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            await semaphore.WaitAsync(ct);
            _ = Task.Run(async () =>
            {
                try
                {
                    await DownloadItemAsync(item, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);
        }
    }

    private async Task DownloadItemAsync(DownloadItem item, CancellationToken globalCt)
    {
        using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);
        _cancellations[item.Id] = itemCts;

        try
        {
            item.Status = DownloadStatus.Downloading;

            // Get file size
            item.TotalBytes = await _api.GetDownloadSizeAsync(item.Type, item.ServerId, itemCts.Token);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(item.DestinationPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            // Support resuming
            long startByte = 0;
            if (File.Exists(item.DestinationPath))
            {
                var existingSize = new FileInfo(item.DestinationPath).Length;
                if (existingSize < item.TotalBytes)
                {
                    startByte = existingSize;
                    item.BytesDownloaded = startByte;
                }
            }

            await using var responseStream = await _api.GetDownloadStreamAsync(
                item.Type, item.ServerId, startByte > 0 ? startByte : null, itemCts.Token);

            var fileMode = startByte > 0 ? FileMode.Append : FileMode.Create;
            await using var fileStream = new FileStream(item.DestinationPath, fileMode, FileAccess.Write, FileShare.None);

            var buffer = new byte[BufferSize];
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, itemCts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), itemCts.Token);
                item.BytesDownloaded += bytesRead;
            }

            item.Status = DownloadStatus.Completed;
            DownloadCompleted?.Invoke(item);
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Paused;
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Failed;
            item.Error = ex.Message;
        }
        finally
        {
            _cancellations.TryRemove(item.Id, out _);
        }
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        _channel.Writer.TryComplete();
        _disposeCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
