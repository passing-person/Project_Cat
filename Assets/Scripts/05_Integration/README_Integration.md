# Project Cat Integration Patch v5-1

This patch fixes world-event target state handling for light switches, printers, and water dispensers.

## Main Rules

- Light switch is not a toggle for the player.
  - Player can only turn the light on when it is currently off.
  - Once triggered, the target becomes Locked.
  - NPC response should turn it off by completing the event through Core.

- Printer / WaterDispenser cannot be mischieved repeatedly while already messy.
  - Once triggered, the target becomes Locked.
  - Cleaner response should complete the event through Core.
  - Default completion disables printer / dispenser for the stage.

## Core Flow

```text
Player uses MischiefTarget
-> PlayerMischiefAction calls CoreFacade.ApplyMischief(context)
-> MischiefWorldEventReporter reports world event
-> CoreFacade resolves nearest NPC / Cleaner
-> Core locks the target
-> NPC reacts
-> NPC calls CoreFacade.CompleteMischiefWorldEvent(targetId)
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

NPC side can receive events with one of these method shapes:

```csharp
public void OnMischiefWorldEvent(MischiefWorldEventContext context)
{
    // React based on context.EventType and context.TargetId.
}
```

Compatibility methods are also supported:

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
