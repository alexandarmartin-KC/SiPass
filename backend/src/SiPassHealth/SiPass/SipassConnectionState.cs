namespace SiPassHealth.SiPass;

public sealed class SipassConnectionState
{
    private readonly object _lock = new();
    public DateTimeOffset? LastSuccessAt { get; private set; }
    public bool IsConnected { get; private set; }
    public bool DataStale { get; private set; }

    public void MarkSuccess(DateTimeOffset timestamp)
    {
        lock (_lock)
        {
            LastSuccessAt = timestamp;
            IsConnected = true;
            DataStale = false;
        }
    }

    public void MarkFailure()
    {
        lock (_lock)
        {
            IsConnected = false;
            DataStale = true;
        }
    }
}
