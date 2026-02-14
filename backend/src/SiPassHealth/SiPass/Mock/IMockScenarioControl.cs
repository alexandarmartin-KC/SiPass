namespace SiPassHealth.SiPass.Mock;

public interface IMockScenarioControl
{
    string Scenario { get; }
    IReadOnlyList<string> Scenarios { get; }
    void SetScenario(string scenario);
}
