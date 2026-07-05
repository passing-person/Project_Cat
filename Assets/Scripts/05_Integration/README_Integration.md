# Project Cat Integration Patch v5-4

This patch changes world events to global NPC broadcast.

## Main Rules

- Light switch is not a toggle for the player.
  - Player can only turn the light on when it is currently off.
  - Once triggered, the target becomes Locked.
  - NPC response should turn it off by completing the event through Core.

- Printer / WaterDispenser cannot be mischieved repeatedly while already messy.
  - Once triggered, the target becomes Locked.
  - Cleaner response should complete the event through Core.
  - Default completion disables printer / dispenser for the stage.

- World events are broadcast to every registered NPC.
  - Core guarantees one unique reactor.
  - Reactor receives `context.ShouldReact == true`.
  - Non-reactors receive `context.ShouldReact == false`.
  - `context.ReactorNpcId` contains the selected reactor id.

## Core Flow

```text
Player uses MischiefTarget
-> PlayerMischiefAction calls CoreFacade.ApplyMischief(context)
-> MischiefWorldEventReporter reports world event
-> Core locks the target immediately
-> Core selects one unique reactor
-> Core broadcasts event to all registered NPCs
-> Reactor performs the main response
-> Non-reactors can play minor reactions or ignore
-> Reactor calls CoreFacade.CompleteMischiefWorldEvent(targetId)
-> Core turns off / clears local target state and updates MischiefTarget state
```

## Core APIs for NPC

```csharp
CoreFacade.RegisterRageReceiver(this);
CoreFacade.ReportPlayerCaught();
CoreFacade.ReduceNpcRage(npcId, amount);
CoreFacade.ReportNpcLostPlayer(npcId);
CoreFacade.CompleteMischiefWorldEvent(targetId);
CoreFacade.CompleteMischiefWorldEvent(targetId, disableTarget, cooldownDuration);
```

## NPC World Event API

Recommended API:

```csharp
public void OnMischiefWorldEvent(MischiefWorldEventContext context)
{
    if (context.ShouldReact)
    {
        // Move to context.Position and resolve the event.
        // When done, call CoreFacade.CompleteMischiefWorldEvent(context.TargetId).
        return;
    }

    // Optional non-reactor behavior: look at the event, play surprise, or ignore.
}
```

Compatibility methods are still supported for the unique reactor only:

```csharp
public void OnLightEvent(string targetId, Vector3 position)
{
    // Move to position, turn off light, then complete.
}

public void OnMessEvent(string targetId, Vector3 position, MischiefWorldEventType eventType)
{
    // Move to position, clean printer/water dispenser, then complete.
}
```

## Reactor Selection

```text
LightToggle        -> nearest non-security NPC gets ShouldReact = true
PrinterMess        -> nearest Cleaner gets ShouldReact = true
WaterDispenserMess -> nearest Cleaner gets ShouldReact = true
GenericMess        -> nearest Cleaner gets ShouldReact = true
```

Every other registered NPC still receives the same event with `ShouldReact = false`.

## Completion Rules

```csharp
// Light: turn off and make switch available again.
coreFacade.CompleteMischiefWorldEvent(targetId, false, 0f);

// Printer / WaterDispenser: clean and disable for this stage.
coreFacade.CompleteMischiefWorldEvent(targetId);

// Optional reusable printer/water dispenser:
coreFacade.CompleteMischiefWorldEvent(targetId, false, 10f);
```

## Scene Builder

Run:

```text
Tools > Project Cat > MainScene > Build Playable MainScene
```

The builder creates:

```text
Keyboard
Phone
WaterDispenser
Printer
LightSwitch
OfficePointLight
```

`OfficePointLight` starts off. Player turns it on. NPC should turn it off by completing the event.

## v5-5 Light / SFX / Microphone Notes

- LightSwitch controls only the explicit `OfficePointLight` reference created by the builder.
- Directional Light is never controlled by LightSwitch and should remain as ambient/base lighting.
- Security spotlights or other additional lights should not be assigned to `MischiefWorldEventReporter.controlledLights`.
- `LightSwitchControlledLight` is added to the office point light as a marker.
- `AudioSfxLibrary` is a clip holder. Assign actual clips to `Assets/ScriptableObjects/04_UISoundCamera/DefaultSfxLibrary.asset`.
- Microphone is now a `MicrophoneBroadcast` world event. It plays `microphone_broadcast_meow`, broadcasts to all NPCs with `ShouldReact = false`, and auto-completes into cooldown.
