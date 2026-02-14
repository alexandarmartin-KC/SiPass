namespace SiPassHealth.SiPass.Mock;

public sealed class MockOptions
{
    public string Scenario { get; set; } = "Normal";
    public int ControllerCount { get; set; } = 5;
    public int AccessPointsPerController { get; set; } = 12;
    public int Seed { get; set; } = 42;
}
