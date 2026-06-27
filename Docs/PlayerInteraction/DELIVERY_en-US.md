# Player / Interaction Module — Delivery Document

**Project:** Project Cat  
**Module:** `Assets/Scripts/02_PlayerInteraction`  
**Test Scene:** `Assets/Scenes/Avery.unity`  
**Unity Version:** 2022.3.43f1c1  
**Delivery Date:** 2026-06-27  
**Owner:** Player / Interaction

---

## 1. Scope

This module covers player cat movement, interaction, mischief, cute action, hiding, and integration with Core systems (`MischiefManager`, `RageManager`, `HidingManager`, etc.).

**Delivered in this phase:**

- Player control scripts (runnable)
- Interactable components (`MischiefTarget`, `HideSpot`)
- Scene build / fix Editor tools
- `Avery.unity` test scene (PlayerCat, keyboard, phone, hide box, supervisor NPC, Core systems)

**Not delivered in this phase (see §8):**

- Production game UI (Canvas)
- Real cat model / Animator / SFX assets
- Tutorial guidance flow and camera direction

---

## 2. Feature Checklist

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | WASD movement | ✅ Done | `PlayerMovement`, Inspector-tunable |
| 2 | Jump (Space) | ✅ Done | Impulse jump, `jumpForce` tunable |
| 3 | Detect interactables | ✅ Done | `OverlapSphere` scan |
| 4 | E contextual interact | ⚠️ Logic done | Unlocks mischief hint on mischief targets; **UI is Console only** |
| 5 | Left-click mischief | ⚠️ Logic done | Wired to `MischiefManager`; **UI is Console only** |
| 6 | Create MischiefContext | ✅ Done | `MischiefTarget.CreateContext()` |
| 7 | Call MischiefManager | ✅ Done | `PlayerMischiefAction` |
| 8 | Q cute action | ✅ Done | Via `CoreFacade`, excludes Security |
| 9 | Cute → RageManager | ✅ Done | `ReduceRageAround` |
| 10 | F hide / exit hide | ✅ Done | `HidingManager`, 10s forced exit |
| 11 | Connect HideSpot | ✅ Done | `HideSpot` + `InteractionZone` |
| 12 | Cat animation | ❌ Stub | `PlayerAnimationController` ready, no Animator assets |
| 13 | Cat SFX | ❌ Stub | `PlayerSfxController` hooks ready, `AudioManager` has no clips |

---

## 3. Controls (Current Build)

| Input | Action | Avery scene defaults |
|-------|--------|----------------------|
| WASD | Move | `moveSpeed = 3` m/s |
| Space | Jump | `jumpForce = 6` |
| E | Interact / unlock mischief hint | `detectionRadius = 1.2m`, `interactionRange = 0.5m` |
| Left Click | Mischief | Requires `IMischiefTarget` as current target |
| Q | Cute / reduce rage | `radius = 4m`, `-20` rage, `cooldown = 5s` (GDD: 20s) |
| F | Hide / exit hide | 1 use per spot per stage, max 10s hide |

> **GDD gaps:** Move speed (GDD 1 m/s vs current 3), cute CD (GDD 20s vs 5), interact range (GDD 0.3m vs scene 0.5m). All tunable in Inspector.

---

## 4. Scene Hierarchy

### PlayerCat

```text
PlayerCat                    ← Scripts, Rigidbody, CapsuleCollider (no mesh)
├── Model                    ← Visual only (MeshFilter + MeshRenderer + PlayerModel)
└── GroundCheck              ← Ground probe
```

### Interactables (example)

```text
Keyboard / Phone / HideSpot_Box
├── Model                    ← Visual only
└── InteractionZone          ← Trigger for interaction range
```

### Systems

```text
AveryTestRoot
├── Environment              ← Ground, desk
├── Systems                  ← Core managers, UIManager, AudioManager
├── PlayerCat
├── Supervisor               ← Test NPC
├── Keyboard
├── Phone
└── HideSpot_Box             ← ~(-2, 0.25, 0)
```

---

## 5. Data Flow

```text
Left-click mischief:
  PlayerMischiefAction
  → MischiefTarget.CreateContext()
  → MischiefManager.ApplyMischief()
  → RageManager / ScoreManager / ObjectiveManager

Q cute action:
  PlayerCuteAction
  → CoreFacade.TryCuteAction()
  → RageManager.ReduceRageAround(excludeSecurity: true)

F hide:
  PlayerHide
  → CoreFacade.ReportPlayerHidden()
  → HidingManager (10s timer, score multiplier ×10%)
  → PlayerController.IsHidden = true (NPCs cannot see player)
```

---

## 6. Script Inventory

| Script | Responsibility |
|--------|----------------|
| `PlayerController` | Player state (controllable, hidden, caught) |
| `PlayerMovement` | WASD, jump |
| `PlayerInteraction` | Target scan, E interact, prompts |
| `PlayerMischiefAction` | Left-click mischief |
| `PlayerCuteAction` | Q cute action |
| `PlayerHide` | F hide |
| `PlayerAnimationController` | Animator parameter API |
| `PlayerSfxController` | SFX ID API |
| `PlayerBootstrap` | Auto-wire scene managers |
| `PlayerBodySetup` | Split Model / fit CapsuleCollider |
| `PlayerModel` | Marks visual-only node |
| `MischiefTarget` | Mischief data + context |
| `HideSpot` | Hide spot |
| `InteractableUtility` | Find IInteractable, distance |
| `InteractableBodySetup` | Interactable Model + InteractionZone |
| `ColliderFitUtility` | Fit collider to renderer |
| `InteractableHighlighter` | Highlight on focus |

### Editor Tools

| Menu | Purpose |
|------|---------|
| `Tools → Project Cat → Build Avery Test Scene` | Build full test scene |
| `Tools → Project Cat → Fix Avery Interaction Colliders` | Fix colliders and Model hierarchy |

---

## 7. Inspector Parameters

### PlayerMovement

- `moveSpeed` — Move speed  
- `jumpForce` — Jump impulse  
- `groundCheckRadius` — Ground check radius  
- `groundLayer` — Ground layer mask  

### PlayerInteraction

- `detectionRadius` — Scan radius (default 1.2)  
- `interactionRange` — Valid interact distance (default 0.5)  
- `logInteractionDebug` — Interaction debug logs  

### PlayerCuteAction

- `radius` — Cute action range (default 4)  
- `rageReduction` — Rage reduction amount (default 20)  
- `cooldown` — Cooldown seconds (default 5)  
- `cooldownUiReportInterval` — Cooldown log interval (default 1s)  

### PlayerBodySetup

- `modelScale` — Model scale (default 0.35)  

### PlayerMischiefAction

- `logMischiefDebug` — Mischief debug logs  

---

## 8. Test Procedure

### 8.1 Scene Setup

1. Open `Assets/Scenes/Avery.unity`  
2. If `AveryTestRoot` is missing, run **Build Avery Test Scene**  
3. Run **Fix Avery Interaction Colliders**  
4. **Ctrl+S** save scene  

### 8.2 Move / Jump

1. Play → WASD move, Space jump  

### 8.3 Interact / Mischief

1. Walk to desk keyboard  
2. Console: `[PlayerInteraction] 扫描选中目标: Keyboard`  
3. **E** → prompt `[左键] 捣乱`  
4. **Left click** → Console: mischief success, supervisor rage increases  

### 8.4 Cute Action

1. Stand near supervisor, press **Q**  
2. Console: cute cooldown logs once per second  

### 8.5 Hide

1. Walk to `HideSpot_Box` on the left (~x = -2)  
2. Console: target `HideSpot_Box`, prompt `[F] 躲藏`  
3. **F** → cannot move, `Hidden - Press F to exit`  
4. **F** again to exit, or wait 10s for forced exit  

---

## 9. Known Issues & Backlog

| Priority | Item | Notes |
|----------|------|-------|
| P0 | Production UI | `UIManager` is Debug.Log only; `ICoreUIBridge` not implemented |
| P0 | World-space [E] prompt | GDD requires on-object prompt; not built |
| P1 | Cat model + Animator | `Model` child is still placeholder capsule |
| P1 | SFX assets | `AudioManager` has no clip mapping |
| P1 | Align GDD values | 1 m/s move, 20s cute CD, 0.3m interact range |
| P2 | PlayerHide debug logs | Failures are silent |
| P2 | Camera follow | Not implemented |
| P2 | Tutorial flow | Not implemented |
| P2 | PlayerCat Prefab | Scene-only, no prefab asset |

---

## 10. Handoff Notes

1. Do **not** put MeshRenderer on `PlayerCat` root — use `Model` child.  
2. Do **not** add SphereCollider on player — interaction uses `OverlapSphere`; physics uses CapsuleCollider only.  
3. Put interactable triggers on `InteractionZone` child, not on squashed model colliders.  
4. Run **Fix Avery Interaction Colliders** after scene edits to repair hierarchy.  
5. Core uses direct manager calls, no Signal Bus (`Assets/Scripts/README.md`).  
6. `PlayerInteractionSensor.cs` was removed; delete orphan `.meta` if Unity reports it and Refresh assets.

---

## 11. Related Paths

```text
Assets/Scripts/02_PlayerInteraction/     ← This module
Assets/Scenes/Avery.unity                  ← Test scene
Assets/Scenes/AveryTestAssets/             ← StageData, NpcData, MischiefTargetData
Docs/PlayerInteraction/DELIVERY_zh-CN.md   ← Chinese delivery doc
```

---

## 12. Dependencies

| Module | Path | Purpose |
|--------|------|---------|
| Core | `01_Core` | Mischief / Rage / Score / Hiding / Fail |
| NPC AI | `03_NPCAI` | Rage, chase, catch, `IsHidden` vision |
| UI / Audio | `04_UISoundCamera` | UIManager, AudioManager (incomplete) |

---

*Document version: v1.0*
