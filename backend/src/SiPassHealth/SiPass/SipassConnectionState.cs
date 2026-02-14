namespace SiPassHealth.SiPass;

public sealed class SipassConnectionState
{
    private readonly object _lock = new();
    public DateTimeOffset? LastSuccessAt { get; private set; }
    public bool IsConnected { get; private set; }
    public bool DataStale { get; private set; }
    public DateTimeOffset? DisconnectedSince { get; private set; }

    public void MarkSuccess(DateTimeOffset timestamp)
    {
        lock (_lock)
        {
            LastSuccessAt = timestamp;
            IsConnected = true;
            DataStale = false;
            DisconnectedSince = null;
        }
    }

    public void MarkFailure()
    {
        lock (_lock)
        {
            IsConnected = false;
            DataStale = true;
            DisconnectedSince ??= DateTimeOffset.UtcNow;
        }
    }

    public void UpdateStatus(bool isConnected, bool dataStale, DateTimeOffset timestamp)
    {
        lock (_lock)
        {
            if (isConnected)
            {
                LastSuccessAt = timestamp;
                IsConnected = true;
                DataStale = dataStale == false ? false : DataStale;
                DisconnectedSince = null;
                return;
            }

            IsConnected = false;
            DataStale = true;
            DisconnectedSince ??= timestamp;
        }
    }
}
