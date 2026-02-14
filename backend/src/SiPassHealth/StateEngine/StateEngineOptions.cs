using System.ComponentModel.DataAnnotations;

namespace SiPassHealth.StateEngine;

public sealed class StateEngineOptions
{
    public DeviceThresholds Controller { get; set; } = new();
    public DeviceThresholds AccessPoint { get; set; } = new();

    [Range(1, 60)]
    public int FlapCountThreshold24h { get; set; } = 6;

    [Range(5, 60)]
    public int TickSeconds { get; set; } = 10;
}

public sealed class DeviceThresholds
{
    [Range(10, 600)]
    public int UnstableSeconds { get; set; } = 120;

    [Range(30, 1200)]
    public int OfflineSeconds { get; set; } = 180;

    [Range(10, 300)]
    public int StableOnlineSeconds { get; set; } = 60;
}
