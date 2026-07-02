using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor; // Required for SceneView
#endif

public class DebugLabelLogic : MonoBehaviour
{
    private Transform targetCameraTransform;
    private NpcController npcController;
    private TMP_Text label;

    private void Awake()
    {
        npcController = GetComponentInParent<NpcController>();
        label = GetComponent<TMP_Text>();

        if (label == null)
            Debug.LogError("DebugLabelLogic: No TMP_Text found on this GameObject.");
        if (npcController == null)
            Debug.LogError("DebugLabelLogic: No NpcController found on parent.");
    }

    private Camera GetTargetCamera()
    {
#if UNITY_EDITOR
        // 1. If the Scene View is active and visible, use its camera.
        //    This is the "floating inspector perspective" you are moving with your mouse.
        if (SceneView.lastActiveSceneView != null)
        {
            return SceneView.lastActiveSceneView.camera;
        }
#endif

        // 2. Fallback to the main Game View camera (for builds or if Scene View is closed).
        return Camera.main;
    }

    private void LateUpdate()
    {
        // Update the cached camera transform every frame (in case you switch views while playing)
        Camera targetCamera = GetTargetCamera();
        if (targetCamera != null)
            targetCameraTransform = targetCamera.transform;
        else
            targetCameraTransform = null;

        // Safety checks
        if (targetCameraTransform == null || npcController == null || label == null)
            return;

        // 1. Face the camera (forward Z points TO the camera)
        //    The "Vector3.up" keeps the text perfectly upright, no tilting.
        transform.LookAt(targetCameraTransform, Vector3.up);
        transform.Rotate(0, 180, 0);

        // 2. Update text with the NPC's current state
        label.text = npcController.CurrentNpcState.ToString();
    }
}