using Microsoft.EntityFrameworkCore;
using SiPassHealth.SiPass;
using SiPassHealth.StateEngine;
using SiPassHealth.Storage;
using SiPassHealth.Web;

namespace SiPassHealth.Workers;

public sealed class SiPassWorker : BackgroundService
{
    private readonly ILogger<SiPassWorker> _logger;
    private readonly SiPassClient _client;
    private readonly SipassConnectionState _connection;
    private readonly StateEngine.StateEngine _stateEngine;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SseBroker _sse;
    private readonly StateEngineOptions _options;
    private readonly SipassOptions _sipassOptions;

    public SiPassWorker(
        ILogger<SiPassWorker> logger,
        SiPassClient client,
        SipassConnectionState connection,
        StateEngine.StateEngine stateEngine,
        IDbContextFactory<AppDbContext> dbFactory,
        SseBroker sse,
        IOptions<StateEngineOptions> options,
        IOptions<SipassOptions> sipassOptions)
    {
        _logger = logger;
        _client = client;
        _connection = connection;
        _stateEngine = stateEngine;
        _dbFactory = dbFactory;
        _sse = sse;
        _options = options.Value;
        _sipassOptions = sipassOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickInterval = TimeSpan.FromSeconds(_options.TickSeconds);
        var renewInterval = TimeSpan.FromSeconds(_sipassOptions.RenewIntervalSeconds);
        var nextRenew = DateTimeOffset.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTimeOffset.UtcNow >= nextRenew)
                {
                    await _client.RenewAsync(stoppingToken);
                    nextRenew = DateTimeOffset.UtcNow.Add(renewInterval);
                }

                await _client.BaselineSyncAsync(stoppingToken);
                _connection.MarkSuccess(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SiPass connection issue");
                _connection.MarkFailure();
                _sse.Publish("sipassConnectionChanged", new { connected = false });
            }

            var changes = _stateEngine.Tick(DateTimeOffset.UtcNow).ToList();
            if (changes.Count > 0)
            {
                foreach (var change in changes)
                {
                    _sse.Publish("statusChanged", change);
                }
            }

            await PersistSnapshotsAsync(stoppingToken);
            await Task.Delay(tickInterval, stoppingToken);
        }
    }

    private async Task PersistSnapshotsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var snapshot = _stateEngine.Snapshot();
        foreach (var item in snapshot)
        {
            var existing = await db.StatusSnapshots.FindAsync(new object?[] { item.ObjectId }, cancellationToken);
            if (existing is null)
            {
                existing = new StatusSnapshot { ObjectId = item.ObjectId };
                db.StatusSnapshots.Add(existing);
            }

            existing.RawComms = item.RawOnline ?? false;
            existing.DerivedState = item.DerivedState;
            existing.OfflineSince = item.OfflineSince;
            existing.LastOnlineAt = item.LastOnlineAt;
            existing.LastSeenAt = item.LastSeenAt;
            existing.FlapCount24h = item.FlapCount24h;
            existing.TotalOfflineSeconds24h = item.TotalOfflineSeconds24h;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
