using System;

/// <summary>
/// Optional hook for the listen-target object. Implement on the object assigned
/// to WaterSplatEffect.listenTarget when it does not use MischiefWorldEventReporter.
/// </summary>
public interface IWaterSplatEffectTrigger
{
    event Action WaterSplatTriggered;
}
