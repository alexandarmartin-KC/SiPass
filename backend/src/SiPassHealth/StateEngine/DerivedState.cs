namespace SiPassHealth.StateEngine;

public enum DerivedState
{
    Ok,
    Unstable,
    Offline,
    Unknown,
    DataStale
}

public enum ObjectType
{
    Controller,
    AccessPoint
}
