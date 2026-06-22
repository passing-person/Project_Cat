# Project Cat Core v3.5

This package contains only Core-side scripts.

It does not include real Player, NPC AI, UI, Sound, Camera, or animation implementation.
Test-only mock scripts are included so Core logic can be verified before other team parts are complete.

## Folder Structure

```text
Assets/Scripts/01_Core/
├── Contexts
├── Data
├── Enums
├── Interfaces
├── Managers
├── Tests
└── Editor
```

## Core Rule

```text
No Signal Bus.
Cross-part communication uses public Core methods.
Data is passed through context structs.
Shared values are stored in ScriptableObjects.
Score, rage, objective, clear, and failure logic belong to Core.
```

External gameplay scripts should not directly modify score, rage, objective, clear, or failure state.

## Recommended External Entry Point

External teams should use `CoreFacade` when possible.

```text
Assets/Scripts/01_Core/Managers/CoreFacade.cs
```

`CoreFacade` wraps the manager calls that Player, NPC, and UI teams need most often.
This reduces direct coupling to several separate Core managers.

### Player / Interaction Usage

When a mischief action succeeds:

```csharp
coreFacade.ApplyMischief(context);
```

When the player uses the cute action:

```csharp
coreFacade.TryCuteAction(playerPosition);
```

When the player is caught:

```csharp
coreFacade.ReportPlayerCaught();
```

Player code should create `MischiefContext` but should not directly change score or rage.

### NPC / AI Usage

Preferred NPC registration:

```csharp
coreFacade.RegisterRageReceiver(this);
```

The NPC should implement:

```text
IRageReceiver
```

Required members:

```text
NpcId
NpcData
CanReceiveRage
Position
SetRageState(state)
StartChase()
StopChase()
LoseTarget()
```

Compatibility registration remains available:

```csharp
coreFacade.RegisterRageReceiver(npcId, this);
```

This allows older `NpcController` scripts to register before they fully implement `IRageReceiver`.

### UI Usage

UI code may implement:

```text
ICoreUIBridge
```

Then register it through:

```csharp
coreFacade.SetUIBridge(this);
```

Core managers can call this bridge without owning the actual UI implementation.

## CoreFacade Public API

Important methods:

```text
ResolveReferences()
WireReferences()
SetUIBridge(bridgeBehaviour)
LoadStage(stageData)
StartStage()
ApplyMischief(context)
CanApplyMischief(targetId)
LockMischiefTarget(targetId)
UnlockMischiefTarget(targetId)
RegisterRageReceiver(receiver)
RegisterRageReceiver(npcId, receiverObject)
UnregisterRageReceiver(receiver)
UnregisterRageReceiver(npcId)
TryCuteAction(origin)
TryCuteAction(origin, radius, rageReduction)
ReportPlayerCaught()
AddScore(amount)
SetSecurityMultiplierOverride(enabled)
SetSecurityMultiplierOverride(enabled, multiplier)
ValidateCoreReferences(out report)
```

## Reference Validation

`CoreReferenceValidator` checks whether required Core managers and important manager links are assigned.

```text
Assets/Scripts/01_Core/Managers/CoreReferenceValidator.cs
```

Use the context menu on the component:

```text
Log Validation
```

Validation treats missing required managers as errors.
Missing UI bridge is a warning because logic-only tests can run without real UI.

## Main Runtime Flow

```text
CoreFacade.ApplyMischief(context)
→ MischiefManager.ApplyMischief(context)
→ RageManager.AddRageByMischief(context)
→ RageManager updates NPC rage
→ RageManager recalculates average rage
→ ScoreManager recalculates score multiplier
→ ScoreManager starts ticking score over time
→ ObjectiveManager updates objective progress
```

## Time-Based Score

Current implementation follows the GDD direction:

```text
Score starts at 0.
ScoreMultiplier starts at 1.0.
When any NPC rage becomes greater than 0, scoring starts.
Score increases over time.
Average NPC rage changes the multiplier.
```

Formula:

```text
AvgRage = sum(Ri) / n
RageFactor = (AvgRage / 100)^2
ScoreMultiplier = 1 + RageFactor * MaxBonus
ScoreGainPerSecond = BaseScoreRate * ScoreMultiplier
```

Security override is supported as a Core-only multiplier override:

```csharp
coreFacade.SetSecurityMultiplierOverride(true, 13f);
coreFacade.SetSecurityMultiplierOverride(false, 13f);
```

This does not implement real Security AI.

## Core Smoke Test

Use this to check whether Core logic works without real Player/NPC/UI code.

### Build test objects in the current scene

Open any scene, then use:

```text
Tools > Project Cat > Core > Build Smoke Test In Current Scene
```

This creates objects in the current active scene only.
It does not create a new scene.

Created objects:

```text
CoreSmokeTest
├── Systems
│   ├── GameManager
│   ├── StageManager
│   ├── ScoreManager
│   ├── RageManager
│   ├── ObjectiveManager
│   ├── FailManager
│   ├── MischiefManager
│   ├── CoreFacade
│   ├── CoreReferenceValidator
│   └── MockUIBridge
├── MockSupervisor
├── MockKeyboardMischiefSource
└── CoreSmokeTestRunner
```

### Run test

Press Play.

Expected Console result:

```text
Core Smoke Test Started
[PASS] ...
Core Smoke Test Finished: PASS X, FAIL 0
```

## What the Smoke Test Checks

- Managers exist.
- `CoreFacade` exists.
- `CoreReferenceValidator` exists.
- Mock supervisor is registered as `IRageReceiver`.
- Score does not tick before rage exists.
- Mischief applies rage to supervisor.
- Score starts after rage exists.
- Score multiplier follows average rage formula.
- 40 rage changes state to Annoyed.
- 70 rage changes state to Angry.
- 100 rage changes state to Enraged.
- 100 rage calls `StartChase()`.
- Target score can be reached.
- `ObjectiveManager.HasEnoughScore()` works.
- `ReduceRage()` lowers rage and updates state.
- `ReduceRageAround()` affects nearby NPCs.
- Locked mischief targets block mischief.
- Security multiplier override forces multiplier to 13 and can be disabled.
- `AlwaysFail` caught rule fails the stage.
- `ClearIfEnoughScore` fails when score is not enough.
- `ClearIfEnoughScore` clears the stage when score is enough.
- `CoreFacade` can route mischief, cute action, target lock, security override, and caught reporting.
- `CoreReferenceValidator` reports valid Core references.

## v3.5 Additions

- Added `CoreFacade` as the recommended external integration entry point.
- Added `CoreReferenceValidator` for scene reference checks.
- Added smoke tests for facade-routed Core calls.
- Added smoke tests for reference validation.
- Updated the smoke scene builder to create and wire `CoreFacade` and `CoreReferenceValidator`.
- Updated integration documentation for Player, NPC, and UI teams.

## Compatibility Note

`IMischiefTarget` intentionally uses the previous contract names:

```text
InteractionId
BaseScore
BaseRageAmount
RageRadius
PrimaryNpcId
```

This keeps existing `02_PlayerInteraction/MischiefTarget.cs` compatible while Core still supports the GDD score/rage system.

`MischiefTargetData` keeps the previous field names `instantScoreBonus` and `baseRageAmount` for compatibility with existing PlayerInteraction code.

## Pending Design Decisions

These are intentionally not hardcoded as final rules yet:

- Whether mischief actions give instant score.
- Whether getting caught always fails.
- Whether enough score allows stage clear after being caught.
- Whether the 30-second timer is demo-only or full-loop.
- Security trigger and final behavior.
