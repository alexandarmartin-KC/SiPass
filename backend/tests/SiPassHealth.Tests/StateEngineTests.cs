using FluentAssertions;
using Microsoft.Extensions.Options;
using SiPassHealth.StateEngine;

namespace SiPassHealth.Tests;

public sealed class StateEngineTests
{
    [Fact]
    public void Controller_DoesNotGoOffline_BeforeThreshold()
    {
        var options = Options.Create(new StateEngineOptions
        {
            Controller = new DeviceThresholds
            {
                UnstableSeconds = 120,
                OfflineSeconds = 180,
                StableOnlineSeconds = 60
            },
            TickSeconds = 10,
            FlapCountThreshold24h = 6
        });

        var engine = new StateEngine(options);
        var now = DateTimeOffset.UtcNow;

        engine.UpdateRawStatus("c1", ObjectType.Controller, false, now);
        for (var i = 0; i < 12; i++)
        {
            engine.Tick(now.AddSeconds(i * 10));
        }

        var status = engine.Snapshot().Single();
        status.DerivedState.Should().Be(DerivedState.Unstable);
    }

    [Fact]
    public void Controller_GoesOffline_AfterThreshold()
    {
        var options = Options.Create(new StateEngineOptions
        {
            Controller = new DeviceThresholds
            {
                UnstableSeconds = 120,
                OfflineSeconds = 180,
                StableOnlineSeconds = 60
            },
            TickSeconds = 10,
            FlapCountThreshold24h = 6
        });

        var engine = new StateEngine(options);
        var now = DateTimeOffset.UtcNow;

        engine.UpdateRawStatus("c1", ObjectType.Controller, false, now);
        engine.Tick(now.AddSeconds(200));

        var status = engine.Snapshot().Single();
        status.DerivedState.Should().Be(DerivedState.Offline);
    }

    [Fact]
    public void Controller_ReturnsToOk_OnlyAfterStableOnline()
    {
        var options = Options.Create(new StateEngineOptions
        {
            Controller = new DeviceThresholds
            {
                UnstableSeconds = 120,
                OfflineSeconds = 180,
                StableOnlineSeconds = 60
            },
            TickSeconds = 10,
            FlapCountThreshold24h = 6
        });

        var engine = new StateEngine(options);
        var now = DateTimeOffset.UtcNow;

        engine.UpdateRawStatus("c1", ObjectType.Controller, false, now);
        engine.Tick(now.AddSeconds(200));
        engine.UpdateRawStatus("c1", ObjectType.Controller, true, now.AddSeconds(200));
        engine.Tick(now.AddSeconds(230));

        var status = engine.Snapshot().Single();
        status.DerivedState.Should().Be(DerivedState.Unstable);

        engine.Tick(now.AddSeconds(270));
        status = engine.Snapshot().Single();
        status.DerivedState.Should().Be(DerivedState.Ok);
    }
}
