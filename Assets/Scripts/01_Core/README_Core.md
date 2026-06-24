# Project Cat Core README

## Current Version: Core v3-6

This package contains Core-only systems for Project Cat.

Core v3-6 adds two integration-safe systems based on the latest GDD:

- Mischief target state support.
- Hiding core support.

No real Player, NPC AI, UI, Camera, Sound, or object implementation is included. Test mocks are included only for Core validation.

## Core Scope

Core owns:

- Score logic.
- Rage logic.
- Objective and fail / clear rules.
- Mischief application rules.
- Mischief target state.
- Hiding score multiplier and hide spot usage tracking.
- Shared data, enums, contexts, and interfaces.
- Core integration facade.
- Core smoke tests.

External teams should integrate through `CoreFacade` whenever possible.

## Main Integration Entry Point

Use `CoreFacade` as the main entry point.

Recommended external calls:

```plain text
Player / Interaction -> CoreFacade.ApplyMischief(context)
NPC / AI             -> CoreFacade.RegisterRageReceiver(receiver)
UI                   -> CoreFacade.SetUIBridge(uiBridge)
Caught event          -> CoreFacade.ReportPlayerCaught()
Cute action           -> CoreFacade.TryCuteAction(position)
Target state          -> CoreFacade.SetMischiefTargetState(targetId, state)
Hiding start          -> CoreFacade.ReportPlayerHidden(hideSpotId)
Hiding end            -> CoreFacade.ReportPlayerExitHiding()
```

## Mischief Target State

The latest tutorial needs target availability states instead of a simple locked/unlocked flag.

Supported states:

```plain text
Available
Cooldown
Disabled
Locked
```

Recommended interpretation:

- `Available`: mischief can be applied.
- `Cooldown`: temporarily unavailable, then returns to `Available` after the cooldown timer.
- `Disabled`: unavailable until manually changed.
- `Locked`: unavailable until manually unlocked.

Useful API:

```plain text
CoreFacade.CanApplyMischief(targetId)
CoreFacade.GetMischiefTargetState(targetId)
CoreFacade.SetMischiefTargetState(targetId, state)
CoreFacade.StartMischiefTargetCooldown(targetId, duration)
CoreFacade.DisableMischiefTarget(targetId)
CoreFacade.LockMischiefTarget(targetId)
CoreFacade.UnlockMischiefTarget(targetId)
```

Compatibility note:

```plain text
LockMischiefTarget and UnlockMischiefTarget still work.
Existing external code using the old lock API should not need to change immediately.
```

## Hiding Core Support

The latest GDD defines hiding spots as temporary escape tools.

Core v3-6 supports:

- Hide spot usage tracking.
- One-time use per stage by default.
- Hidden state tracking.
- Max hiding duration.
- Forced exit after the max duration.
- Temporary score multiplier scaling while hidden.

Default values from StageData:

```plain text
maxHideDuration = 10 seconds
hiddenMultiplierScale = 0.1
hideSpotUsesPerStage = 1
```

Useful API:

```plain text
CoreFacade.CanUseHideSpot(hideSpotId)
CoreFacade.ReportPlayerHidden(hideSpotId)
CoreFacade.ReportPlayerExitHiding()
CoreFacade.HasUsedHideSpot(hideSpotId)
```

Score behavior:

```plain text
While hidden, ScoreManager applies TemporaryMultiplierScale.
CurrentMultiplier = CurrentBaseMultiplier * TemporaryMultiplierScale.
```

NPC movement behavior is not implemented by Core. NPC / AI should handle the GDD-specific responses:

```plain text
Supervisor / Worker: walk back to original position.
Cleaner: continue cleaning.
Security: stay in place.
```

## Smoke Test

Build the smoke test in the current scene:

```plain text
Tools > Project Cat > Core > Build Smoke Test In Current Scene
```

Then press Play.

Expected result:

```plain text
Core Smoke Test Finished: PASS X, FAIL 0
```

The exact pass count may change as tests are added. The important condition is `FAIL 0`.

## Current Next Steps

Core v3-6 is intended to support the current tutorial direction before full integration.

Next external integration targets:

- Player / Interaction: scripted tutorial sequence and target interaction.
- UI: staged prompts, score UI, rage UI, cute prompt, hiding prompt.
- NPC / AI: chase start, chase stop, and hiding response behavior.
- Sound / Camera: BGM change and camera focus moments.

Core should not implement those external systems directly.

---
