using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SiPassHealth.SiPass;

public sealed class SipassOptions
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool VerifyTls { get; set; } = true;
    public int RenewIntervalSeconds { get; set; } = 90;
}

public sealed class SiPassClient
{
    private readonly HttpClient _httpClient;
    private readonly SipassOptions _options;

    public SiPassClient(HttpClient httpClient, IOptions<SipassOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public Task LoginAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement MS-API login call.
        return Task.CompletedTask;
    }

    public Task RenewAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement MS-API session renew.
        return Task.CompletedTask;
    }

    public Task BaselineSyncAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement baseline status sync.
        return Task.CompletedTask;
    }

    public Task SubscribeSignalRAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement SignalR subscription and message handling.
        return Task.CompletedTask;
    }
}
