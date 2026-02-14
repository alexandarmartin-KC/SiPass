using System.Collections.Concurrent;

namespace SiPassHealth.StateEngine;

public sealed class StateEngine
{
    private readonly StateEngineOptions _options;
    private readonly ConcurrentDictionary<string, ObjectStatus> _states = new();

    public StateEngine(IOptions<StateEngineOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyCollection<ObjectStatus> Snapshot()
    {
        return _states.Values.ToArray();
    }

    public void UpdateRawStatus(string objectId, ObjectType type, bool isOnline, DateTimeOffset seenAt)
    {
        var status = _states.GetOrAdd(objectId, _ => new ObjectStatus(objectId, type));
        status.LastSeenAt = seenAt;
        status.RawOnline = isOnline;

        if (isOnline)
        {
            status.LastOnlineAt = seenAt;
            if (status.OfflineSince is not null)
            {
                status.OfflineSince = null;
            }
        }
        else
        {
            status.OfflineSince ??= seenAt;
        }
    }

    public IEnumerable<ObjectStatus> Tick(DateTimeOffset now)
    {
        var changes = new List<ObjectStatus>();
        foreach (var status in _states.Values)
        {
            var previous = status.DerivedState;
            status.DerivedState = Evaluate(status, now);

            if (previous != status.DerivedState)
            {
                status.RecordTransition(now);
                changes.Add(status);
            }

            if (status.RawOnline == false)
            {
                status.AddOfflineSample(now, _options.TickSeconds);
            }

            status.TrimHistory(now);
        }

        return changes;
    }

    private DerivedState Evaluate(ObjectStatus status, DateTimeOffset now)
    {
        if (status.LastSeenAt is null)
        {
            return DerivedState.Unknown;
        }

        var thresholds = status.Type == ObjectType.Controller ? _options.Controller : _options.AccessPoint;

        if (status.RawOnline == false)
        {
            var offlineFor = status.OfflineSince is null ? TimeSpan.Zero : now - status.OfflineSince.Value;
            if (offlineFor.TotalSeconds >= thresholds.OfflineSeconds)
            {
                return DerivedState.Offline;
            }

            return DerivedState.Unstable;
        }

        if (status.LastOnlineAt is null)
        {
            return DerivedState.Unknown;
        }

        var onlineFor = now - status.LastOnlineAt.Value;
        if (onlineFor.TotalSeconds < thresholds.StableOnlineSeconds)
        {
            return DerivedState.Unstable;
        }

        if (status.FlapCount24h > _options.FlapCountThreshold24h)
        {
            return DerivedState.Unstable;
        }

        return DerivedState.Ok;
    }
}

public sealed class ObjectStatus
{
    private readonly Queue<DateTimeOffset> _transitions = new();
    private readonly Queue<OfflineSample> _offlineSamples = new();

    public ObjectStatus(string objectId, ObjectType type)
    {
        ObjectId = objectId;
        Type = type;
    }

    public string ObjectId { get; }
    public ObjectType Type { get; }
    public bool? RawOnline { get; set; }
    public DerivedState DerivedState { get; set; } = DerivedState.Unknown;
    public DateTimeOffset? OfflineSince { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public int FlapCount24h => _transitions.Count;
    public int TotalOfflineSeconds24h { get; private set; }

    public void RecordTransition(DateTimeOffset timestamp)
    {
        _transitions.Enqueue(timestamp);
    }

    public void AddOfflineSample(DateTimeOffset timestamp, int seconds)
    {
        _offlineSamples.Enqueue(new OfflineSample(timestamp, seconds));
        TotalOfflineSeconds24h += seconds;
    }

    public void TrimHistory(DateTimeOffset now)
    {
        while (_transitions.Count > 0 && (now - _transitions.Peek()).TotalHours > 24)
        {
            _transitions.Dequeue();
        }

        while (_offlineSamples.Count > 0 && (now - _offlineSamples.Peek().Timestamp).TotalHours > 24)
        {
            var sample = _offlineSamples.Dequeue();
            TotalOfflineSeconds24h = Math.Max(0, TotalOfflineSeconds24h - sample.Seconds);
        }
    }
}

public sealed record OfflineSample(DateTimeOffset Timestamp, int Seconds);
