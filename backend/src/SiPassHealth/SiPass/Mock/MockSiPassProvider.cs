using Microsoft.Extensions.Options;
using SiPassHealth.StateEngine;

namespace SiPassHealth.SiPass.Mock;

public sealed class MockSiPassProvider : ISiPassProvider, IMockScenarioControl
{
    private readonly MockOptions _options;
    private readonly Random _random;
    private readonly List<MockDevice> _devices = new();
    private bool _flipFlap;
    private bool _recoverDone;
    private bool _massReconnectDone;
    private string _scenario;

    public string Scenario => _scenario;
    public IReadOnlyList<string> Scenarios { get; } = new[]
    {
        "Normal",
        "SingleControllerOffline",
        "Flapping",
        "SystemicOutage",
        "DataStale",
        "Recover",
        "MassReconnect",
        "Dynamic"
    };

    public MockSiPassProvider(IOptions<MockOptions> options)
    {
        _options = options.Value;
        _random = new Random(_options.Seed);
        _scenario = NormalizeScenario(_options.Scenario);
        SeedDevices();
    }

    public void SetScenario(string scenario)
    {
        _scenario = NormalizeScenario(scenario);
        _flipFlap = false;
        _recoverDone = false;
        _massReconnectDone = false;
    }

    public Task RenewAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<SiPassSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var scenario = _scenario.ToLowerInvariant();

        if (scenario == "datastale")
        {
            return Task.FromResult(new SiPassSnapshot(
                IsConnected: false,
                DataStale: true,
                SnapshotAt: now,
                Devices: _devices.Select(device => device.ToSnapshot()).ToList()));
        }

        ApplyScenario(scenario, now);

        return Task.FromResult(new SiPassSnapshot(
            IsConnected: true,
            DataStale: false,
            SnapshotAt: now,
            Devices: _devices.Select(device => device.ToSnapshot()).ToList()));
    }

    private void SeedDevices()
    {
        var controllerCount = Math.Max(1, _options.ControllerCount);
        var accessPointsPerController = Math.Max(1, _options.AccessPointsPerController);

        for (var controllerIndex = 1; controllerIndex <= controllerCount; controllerIndex++)
        {
            var controllerId = $"CTRL-{controllerIndex:00}";
            var controllerName = $"Controller {controllerIndex:00}";
            var controllerPath = controllerIndex % 2 == 0
                ? $"Site A / Zone {controllerIndex:00}"
                : $"Site B / Zone {controllerIndex:00}";

            _devices.Add(new MockDevice(controllerId, ObjectType.Controller, controllerName, controllerPath, null));

            for (var apIndex = 1; apIndex <= accessPointsPerController; apIndex++)
            {
                var accessPointId = $"AP-{controllerIndex:00}-{apIndex:00}";
                var accessPointName = $"Access Point {controllerIndex:00}-{apIndex:00}";
                var accessPointPath = $"{controllerPath} / Door {apIndex:00}";

                _devices.Add(new MockDevice(
                    accessPointId,
                    ObjectType.AccessPoint,
                    accessPointName,
                    accessPointPath,
                    controllerId));
            }
        }
    }

    private void ApplyScenario(string scenario, DateTimeOffset now)
    {
        switch (scenario)
        {
            case "singlecontrolleroffline":
                ApplySingleControllerOffline(now);
                break;
            case "flapping":
                ApplyFlapping(now);
                break;
            case "systemicoutage":
                ApplySystemicOutage(now);
                break;
            case "recover":
                ApplyRecover(now);
                break;
            case "massreconnect":
                ApplyMassReconnect(now);
                break;
            case "dynamic":
                ApplyDynamic(now);
                break;
            default:
                ApplyNormal(now);
                break;
        }
    }

    private string NormalizeScenario(string scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario))
        {
            return "Normal";
        }

        var trimmed = scenario.Trim();
        var match = Scenarios.FirstOrDefault(item =>
            string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new ArgumentException("Unknown scenario", nameof(scenario));
        }

        return match;
    }

    private void ApplyNormal(DateTimeOffset now)
    {
        foreach (var device in _devices)
        {
            device.MarkOnline(now);
        }
    }

    private void ApplySingleControllerOffline(DateTimeOffset now)
    {
        var targetController = _devices.FirstOrDefault(device => device.Type == ObjectType.Controller);
        if (targetController is null)
        {
            ApplyNormal(now);
            return;
        }

        var offlineSince = now.AddMinutes(-45);

        foreach (var device in _devices)
        {
            if (device.ObjectId == targetController.ObjectId || device.ParentId == targetController.ObjectId)
            {
                device.MarkOffline(offlineSince);
            }
            else
            {
                device.MarkOnline(now);
            }
        }
    }

    private void ApplyFlapping(DateTimeOffset now)
    {
        ApplyNormal(now);

        var flappingDevice = _devices.FirstOrDefault(device => device.Type == ObjectType.AccessPoint)
            ?? _devices.First();

        _flipFlap = !_flipFlap;
        if (_flipFlap)
        {
            flappingDevice.MarkOffline(now.AddSeconds(-30));
        }
        else
        {
            flappingDevice.MarkOnline(now);
        }
    }

    private void ApplySystemicOutage(DateTimeOffset now)
    {
        var controllers = _devices.Where(device => device.Type == ObjectType.Controller).ToList();
        if (controllers.Count == 0)
        {
            ApplyNormal(now);
            return;
        }

        var offlineCount = (int)Math.Ceiling(controllers.Count * 0.8);
        var offlineControllers = controllers.OrderBy(_ => _random.Next()).Take(offlineCount).ToHashSet();
        var offlineSince = now.AddSeconds(-90);

        foreach (var device in _devices)
        {
            if (offlineControllers.Any(ctrl => ctrl.ObjectId == device.ObjectId || ctrl.ObjectId == device.ParentId))
            {
                device.MarkOffline(offlineSince);
            }
            else
            {
                device.MarkOnline(now);
            }
        }
    }

    private void ApplyRecover(DateTimeOffset now)
    {
        var target = _devices.FirstOrDefault(device => device.Type == ObjectType.Controller)
            ?? _devices.First();

        if (!_recoverDone)
        {
            target.MarkOffline(now.AddMinutes(-15));
            _recoverDone = true;
        }
        else
        {
            target.MarkOnline(now);
        }
    }

    private void ApplyMassReconnect(DateTimeOffset now)
    {
        if (!_massReconnectDone)
        {
            foreach (var device in _devices)
            {
                device.MarkOffline(now.AddMinutes(-5));
            }
            _massReconnectDone = true;
        }
        else
        {
            foreach (var device in _devices)
            {
                device.MarkOnline(now);
            }
        }
    }

    private void ApplyDynamic(DateTimeOffset now)
    {
        ApplyNormal(now);

        if (_devices.Count == 0)
        {
            return;
        }

        var randomDevice = _devices[_random.Next(_devices.Count)];
        var roll = _random.NextDouble();
        if (roll < 0.02)
        {
            randomDevice.MarkOffline(now.AddSeconds(-60));
            return;
        }

        if (roll < 0.05)
        {
            _flipFlap = !_flipFlap;
            if (_flipFlap)
            {
                randomDevice.MarkOffline(now.AddSeconds(-15));
            }
            else
            {
                randomDevice.MarkOnline(now);
            }
        }
    }

    private sealed class MockDevice
    {
        public MockDevice(string objectId, ObjectType type, string name, string path, string? parentId)
        {
            ObjectId = objectId;
            Type = type;
            Name = name;
            Path = path;
            ParentId = parentId;
            LastSeenAt = DateTimeOffset.UtcNow;
            IsOnline = true;
        }

        public string ObjectId { get; }
        public ObjectType Type { get; }
        public string Name { get; }
        public string Path { get; }
        public string? ParentId { get; }
        public bool IsOnline { get; private set; }
        public DateTimeOffset LastSeenAt { get; private set; }

        public void MarkOnline(DateTimeOffset now)
        {
            IsOnline = true;
            LastSeenAt = now;
        }

        public void MarkOffline(DateTimeOffset since)
        {
            IsOnline = false;
            LastSeenAt = since;
        }

        public SiPassDeviceSnapshot ToSnapshot()
        {
            return new SiPassDeviceSnapshot(ObjectId, Type, Name, Path, ParentId, IsOnline, LastSeenAt);
        }
    }
}
