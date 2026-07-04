# Project Cat Integration v2

This patch focuses on playable feedback for the submission build.

## Main scene rule

Use this scene as the playable integration scene:

```text
Assets/Scenes/MainScene.unity
```

Build or rebuild the functional scene with:

```text
Tools > Project Cat > MainScene > Build Playable MainScene
```

## Added / changed systems

```text
UIManager
- Creates a runtime Canvas if none exists.
- Shows score, target score, multiplier, rage, target prompt, key hints, hide timer, hidden overlay, clear/fail result.
- Implements ICoreUIBridge.

PlayerInteraction
- Sends current target and action state to UIManager.
- Shows whether E selection or LMB mischief is expected.

PlayerMischiefAction
- Sends visible feedback after successful mischief.

PlayerHide
- Shows a dark hidden overlay and remaining hide timer while hidden.

InteractableHighlighter
- Auto-generates a small marker if no custom highlight object exists.
- Tints nearby interactable objects while selected.

ThirdPersonCameraController
- Replaces prototype first-person look for MainScene.
- Supports mouse left/right and up/down TPS camera movement.
- Rotates the player yaw so WASD stays camera-facing.
```

## Test loop

1. Open `Assets/Scenes/MainScene.unity`.
2. Press Play.
3. Move with WASD and rotate camera with Mouse.
4. Approach Keyboard / Phone / WaterDispenser.
5. Check target panel and highlight marker.
6. Press E, then Left Click.
7. Check score, multiplier, rage bar, and feedback message.
8. Approach HideSpot_Box and press F.
9. Check hidden overlay and hide timer.
10. Wait 10 seconds and confirm forced exit.

## Scope note

This patch does not modify `Assets/Scripts/03_NPCAI/`.
