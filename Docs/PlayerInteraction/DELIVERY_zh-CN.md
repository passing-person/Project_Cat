# Player / Interaction 模块交付文档

**项目：** Project Cat  
**模块：** `Assets/Scripts/02_PlayerInteraction`  
**测试场景：** `Assets/Scenes/Avery.unity`  
**Unity 版本：** 2022.3.43f1c1  
**交付日期：** 2026-06-27  
**负责人：** Player / Interaction

---

## 1. 交付范围

本模块负责玩家猫的移动、交互、捣乱、撒娇、躲藏，以及与 Core 系统（`MischiefManager`、`RageManager`、`HidingManager` 等）的对接。

**本阶段交付内容：**

- 玩家控制逻辑脚本（完整可运行）
- 可交互物体组件（`MischiefTarget`、`HideSpot`）
- 场景搭建与修复 Editor 工具
- `Avery.unity` 测试场景（含 PlayerCat、键盘、电话、纸箱躲藏点、主管 NPC、Core Systems）

**本阶段未交付（见第 8 节）：**

- 正式游戏 UI（Canvas）
- 真实猫模型 / Animator / 音效资源
- 教学关引导流程与运镜

---

## 2. 功能清单与状态

| # | 功能 | 状态 | 说明 |
|---|------|------|------|
| 1 | WASD 移动 | ✅ 完成 | `PlayerMovement`，Inspector 可调 |
| 2 | 跳跃 (Space) | ✅ 完成 | 冲量跳跃，`jumpForce` 可调 |
| 3 | 检测可交互目标 | ✅ 完成 | `OverlapSphere` 扫描 |
| 4 | E 上下文交互 | ⚠️ 逻辑完成 | 捣乱点按 E 解锁左键提示；**UI 仅 Console** |
| 5 | 左键捣乱 | ⚠️ 逻辑完成 | 已接 `MischiefManager`；**UI 仅 Console** |
| 6 | 创建 MischiefContext | ✅ 完成 | `MischiefTarget.CreateContext()` |
| 7 | 调用 MischiefManager | ✅ 完成 | `PlayerMischiefAction` |
| 8 | Q 撒娇 | ✅ 完成 | 经 `CoreFacade`，排除保安 |
| 9 | 撒娇接 RageManager | ✅ 完成 | `ReduceRageAround` |
| 10 | F 躲藏 / 出箱 | ✅ 完成 | 接 `HidingManager`，10 秒强制出箱 |
| 11 | 连接 HideSpot | ✅ 完成 | `HideSpot` + `InteractionZone` |
| 12 | 猫动画 | ❌ 占位 | `PlayerAnimationController` 接口就绪，无 Animator 资源 |
| 13 | 猫音效 | ❌ 占位 | `PlayerSfxController` 调用点就绪，`AudioManager` 无 Clip |

---

## 3. 操作说明（当前实现）

| 按键 | 功能 | 默认参数（Avery 场景） |
|------|------|------------------------|
| WASD | 移动 | `moveSpeed = 3` m/s |
| Space | 跳跃 | `jumpForce = 6` |
| E | 交互 / 解锁捣乱提示 | 检测半径 `1.2m`，有效距离 `0.5m` |
| 左键 | 捣乱 | 需当前目标为 `IMischiefTarget` |
| Q | 撒娇降怒 | 半径 `4m`，怒气 `-20`，CD `5s`（GDD 为 20s，待对齐） |
| F | 躲藏 / 离开躲藏点 | 每关每点可用 1 次，最长 10 秒 |

> **与 GDD 差异：** 移动速度（GDD 1 m/s vs 当前 3）、撒娇 CD（GDD 20s vs 当前 5s）、交互距离（GDD 0.3m vs 场景 0.5m）可在 Inspector 调整。

---

## 4. 场景 Hierarchy 结构

### PlayerCat

```text
PlayerCat                    ← 脚本、Rigidbody、CapsuleCollider（无模型）
├── Model                    ← 仅视觉（MeshFilter + MeshRenderer + PlayerModel）
└── GroundCheck              ← 地面检测点
```

### 可交互物体（示例）

```text
Keyboard / Phone / HideSpot_Box
├── Model                    ← 仅视觉
└── InteractionZone          ← Trigger，用于交互范围检测
```

### 系统

```text
AveryTestRoot
├── Environment              ← 地面、桌子
├── Systems                  ← Core Managers、UIManager、AudioManager
├── PlayerCat
├── Supervisor               ← 测试用 NPC
├── Keyboard
├── Phone
└── HideSpot_Box             ← 位置约 (-2, 0.25, 0)
```

---

## 5. 核心数据流

```text
左键捣乱:
  PlayerMischiefAction
  → MischiefTarget.CreateContext()
  → MischiefManager.ApplyMischief()
  → RageManager / ScoreManager / ObjectiveManager

Q 撒娇:
  PlayerCuteAction
  → CoreFacade.TryCuteAction()
  → RageManager.ReduceRageAround(excludeSecurity: true)

F 躲藏:
  PlayerHide
  → CoreFacade.ReportPlayerHidden()
  → HidingManager（10s 计时、分数倍率 ×10%）
  → PlayerController.IsHidden = true（NPC 视野失效）
```

---

## 6. 脚本清单

| 脚本 | 职责 |
|------|------|
| `PlayerController` | 玩家状态（可控、躲藏、被抓住） |
| `PlayerMovement` | WASD、跳跃 |
| `PlayerInteraction` | 目标扫描、E 交互、提示 |
| `PlayerMischiefAction` | 左键捣乱 |
| `PlayerCuteAction` | Q 撒娇 |
| `PlayerHide` | F 躲藏 |
| `PlayerAnimationController` | Animator 参数接口 |
| `PlayerSfxController` | 音效 ID 接口 |
| `PlayerBootstrap` | 自动连接场景 Manager |
| `PlayerBodySetup` | 分离 Model / 对齐 CapsuleCollider |
| `PlayerModel` | 标记纯模型节点 |
| `MischiefTarget` | 捣乱点数据与 Context |
| `HideSpot` | 躲藏点 |
| `InteractableUtility` | 查找 IInteractable、距离计算 |
| `InteractableBodySetup` | 可交互物 Model + InteractionZone |
| `ColliderFitUtility` | Collider 贴合 Renderer |
| `InteractableHighlighter` | 高亮显示 |

### Editor 工具

| 菜单 | 功能 |
|------|------|
| `Tools → Project Cat → Build Avery Test Scene` | 搭建完整测试场景 |
| `Tools → Project Cat → Fix Avery Interaction Colliders` | 修复碰撞体与 Model 层级 |

---

## 7. Inspector 可调参数

### PlayerMovement

- `moveSpeed` — 移动速度  
- `jumpForce` — 跳跃力度  
- `groundCheckRadius` — 地面检测半径  
- `groundLayer` — 地面 Layer  

### PlayerInteraction

- `detectionRadius` — 扫描半径（默认 1.2）  
- `interactionRange` — 有效交互距离（默认 0.5）  
- `logInteractionDebug` — 交互 Debug 日志  

### PlayerCuteAction

- `radius` — 撒娇范围（默认 4）  
- `rageReduction` — 怒气减少量（默认 20）  
- `cooldown` — 冷却秒数（默认 5）  
- `cooldownUiReportInterval` — 冷却日志间隔（默认 1 秒）  

### PlayerBodySetup

- `modelScale` — 模型缩放（默认 0.35）  

### PlayerMischiefAction

- `logMischiefDebug` — 捣乱 Debug 日志  

---

## 8. 测试步骤

### 8.1 场景准备

1. 打开 `Assets/Scenes/Avery.unity`  
2. 若 Hierarchy 无 `AveryTestRoot`，运行 **Build Avery Test Scene**  
3. 运行 **Fix Avery Interaction Colliders**  
4. **Ctrl+S** 保存场景  

### 8.2 移动 / 跳跃

1. Play → WASD 移动，Space 跳跃  

### 8.3 交互 / 捣乱

1. 走到桌子键盘旁  
2. Console：`[PlayerInteraction] 扫描选中目标: Keyboard`  
3. **E** → 提示 `[左键] 捣乱`  
4. **左键** → Console：`左键成功：捣乱 → Keyboard`，主管怒气上升  

### 8.4 撒娇

1. 靠近主管按 **Q**  
2. Console：`撒娇冷却: 5.0s / 5.0s`（每秒更新一次）  

### 8.5 躲藏

1. 走到左侧 `HideSpot_Box`（约 x = -2）  
2. Console：`扫描选中目标: HideSpot_Box`，`[F] 躲藏`  
3. **F** → 不能移动，Console：`Hidden - Press F to exit`  
4. 再按 **F** 出箱，或等待 10 秒强制出箱  

---

## 9. 已知问题与待办

| 优先级 | 项 | 说明 |
|--------|-----|------|
| P0 | 正式 UI | `UIManager` 仅 Debug.Log，未实现 `ICoreUIBridge` |
| P0 | 世界空间 [E] 提示 | GDD 要求在物体上显示，当前无 |
| P1 | 猫模型 + Animator | `Model` 子物体仍为占位 Capsule |
| P1 | 音效资源 | `AudioManager` 无 AudioClip 映射 |
| P1 | GDD 数值对齐 | 速度 1 m/s、撒娇 CD 20s、交互 0.3m |
| P2 | PlayerHide Debug 日志 | 失败时静默，建议补充 |
| P2 | 相机跟随 | 未实现 |
| P2 | 教学关引导 | 未实现 |
| P2 | PlayerCat Prefab | 仅存在于场景中，未做成 Prefab |

---

## 10. 接手人注意事项

1. **不要**在 `PlayerCat` 根物体上挂 MeshRenderer，模型放 `Model` 子物体。  
2. **不要**在玩家上添加 SphereCollider；交互靠 `OverlapSphere`，物理只需 CapsuleCollider。  
3. 可交互物体的 Trigger 放在 `InteractionZone` 子物体，不要贴在压扁的 Model 上。  
4. 修改场景后运行 **Fix Avery Interaction Colliders** 可快速修复层级。  
5. Core 通信走 Manager 直调，不用 Signal Bus（见 `Assets/Scripts/README.md`）。  
6. `PlayerInteractionSensor.cs` 已删除，若 Unity 报 orphan `.meta`，删除对应 `.meta` 并 Refresh。

---

## 11. 相关资源路径

```text
Assets/Scripts/02_PlayerInteraction/     ← 本模块脚本
Assets/Scenes/Avery.unity                  ← 测试场景
Assets/Scenes/AveryTestAssets/             ← StageData、NpcData、MischiefTargetData
Docs/PlayerInteraction/DELIVERY_en-US.md   ← 英文版交付文档
```

---

## 12. 联系人 / 依赖

| 依赖模块 | 路径 | 用途 |
|----------|------|------|
| Core | `01_Core` | Mischief / Rage / Score / Hiding / Fail |
| NPC AI | `03_NPCAI` | 怒气、追逐、抓捕、`IsHidden` 视野 |
| UI / Audio | `04_UISoundCamera` | UIManager、AudioManager（待完善） |

---

*文档版本：v1.0*
