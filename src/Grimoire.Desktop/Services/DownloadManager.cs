using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Desktop.Services;

public record DownloadItem(
    string Id,
    string Name,
    DownloadableType Type,
    int ServerId,
    string DestinationPath
)
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public double ProgressPercent => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100.0 : 0.0;
    public string? Error { get; set; }
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
