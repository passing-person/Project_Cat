# Project Cat Core Contract v2

This package contains the basic Unity C# scripts for the current MVP implementation.

## Folder Structure

```text
Assets/Scripts/
├── 01_Core
├── 02_PlayerInteraction
├── 03_NPCAI
└── 04_UISoundCamera
```

## Main Rule

No Signal Bus is used.

Cross-system communication should use direct manager method calls and context structs.

```text
Player / Interactable
→ MischiefManager.ApplyMischief(MischiefContext context)
→ RageManager
→ ScoreManager
→ ObjectiveManager / FailManager
→ UIManager
```

## 01_Core

Owner: Head Programmer / Core

Contains:

- Shared enums
- Context structs
- ScriptableObject data assets
- Interfaces
- Core managers

Core owns:

- Score
- Rage
- Objective checks
- Stage clear / fail result
- Shared data rules

Other systems should not directly modify score or rage.

## 02_PlayerInteraction

Owner: Player / Interaction Programmer

Contains basic placeholders for:

- Player movement
- Player interaction detection
- Mischief action
- Cute action
- Hiding
- MischiefTarget
- HideSpot

Required integration:

```text
Create MischiefContext
Call MischiefManager.ApplyMischief(context)
```

## 03_NPCAI

Owner: NPC / AI Programmer

Contains basic placeholders for:

- NpcController
- NpcPerception
- NpcChase
- NpcCatch

Required integration:

```text
NpcId
NpcData
CanReceiveRage
SetRageState(state)
StartChase()
StopChase()
LoseTarget()
```

When the player is caught, call:

```text
FailManager.HandlePlayerCaught()
```

## 04_UISoundCamera

Owner: UI / Sound / Camera Programmer

Contains basic placeholders for:

- UIManager
- AudioManager
- CameraManager

UI should display manager values only. UI should not decide game rules.

## GDD-Based Defaults

Use these values unless the design side updates them:

| Feature | Value |
|---|---|
| Move speed | 4 units/sec |
| Stomp rage increase | +5% |
| Cute rage reduction | -20% |
| Cute cooldown | 10 sec |
| Cute tutorial threshold | 40% rage |
| Keyboard lock threshold | 70% rage |
| Chase threshold | 100% rage |
| Survival time | 30 sec |
| Base score rate | 10 points/sec |
| Max multiplier bonus | 12 |
| Security multiplier | 13 |

## Still Needs Design Confirmation

Do not hard-code these until confirmed:

- Final clear/fail rule
- Whether mischief gives instant score
- Security spawn trigger
- Whether Electrical Control Room is required or optional
- Full stage order
- Which non-MVP objects are required now

## Minimal MVP Test

MVP is ready when:

- Cat can move and jump.
- Cat can detect keyboard or phone.
- Mischief increases NPC rage.
- Score starts increasing over time after rage appears.
- Score multiplier changes from average NPC rage.
- Supervisor starts chase at 100% rage.
- Cat can hide.
- NPC loses target or fails to catch hidden cat.
- Caught result goes through `FailManager.HandlePlayerCaught()`.
