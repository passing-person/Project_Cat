# Project Cat Integration v3

This patch focuses on emergency visual feedback and MainScene play testing.

## Scope

Modified areas:

```text
Assets/Scripts/01_Core/
Assets/Scripts/02_PlayerInteraction/
Assets/Scripts/04_UISoundCamera/
Assets/Scripts/05_Integration/
```

Not modified:

```text
Assets/Scripts/03_NPCAI/
```

## MainScene workflow

Use this menu before testing:

```text
Tools > Project Cat > MainScene > Build Playable MainScene
```

The builder creates or repairs:

```text
Core runtime managers
Runtime UI feedback
Runtime feedback audio
TPS camera
Player
Mischief targets
Hide spot
Basic floor / walls
NavMeshSurface bake if AI Navigation package is available
NavMeshAgentPlacementFixer
```

## Player controls

```text
WASD    Move
Shift   Sprint
Mouse   Camera
Space   Jump
E       Select / interact
LMB     Mischief
Q       Cute
F       Hide / exit hide
Esc     Unlock cursor
```

## Required NPC-side connection

NPC code still needs to call this when the player is caught:

```csharp
CoreFacade.ReportPlayerCaught();
```

## NavMesh note

If NPC logs this error:

```text
SetDestination can only be called on an active agent that has been placed on a NavMesh.
```

Run the MainScene builder again. It will try to bake a NavMesh using the AI Navigation package. If the package is missing or the bake fails, bake the NavMesh manually from the NavMeshSurface object in MainScene.
