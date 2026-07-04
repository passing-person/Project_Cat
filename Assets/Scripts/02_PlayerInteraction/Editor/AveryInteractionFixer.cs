#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AveryInteractionFixer
{
    [MenuItem("Tools/Project Cat/Fix Avery Interaction Colliders")]
    public static void FixCurrentScene()
    {
        FixPlayer();
        FixInteractable("Keyboard", new Vector3(0f, 10f, 0f), new Vector3(1.4f, 1.4f, 1.4f));
        FixInteractable("Phone", new Vector3(0f, 8f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
        FixInteractable("HideSpot_Box", Vector3.zero, new Vector3(1.4f, 1.2f, 1.4f));

        Debug.Log("已修复：PlayerCat/Model 仅含模型，根物体仅含 CapsuleCollider。请 Ctrl+S 保存场景。");
        EditorSceneManagerBridge.MarkCurrentSceneDirty();
    }

    private static void FixPlayer()
    {
        GameObject player = GameObject.Find("PlayerCat");
        if (player == null)
        {
            Debug.LogWarning("未找到 PlayerCat。");
            return;
        }

        PlayerBodySetup bodySetup = player.GetComponent<PlayerBodySetup>();
        if (bodySetup == null)
            bodySetup = player.AddComponent<PlayerBodySetup>();

        bodySetup.modelScale = 0.35f;
        bodySetup.Apply();

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.detectionRadius = 1.2f;
            interaction.interactionRange = 0.5f;
            interaction.logInteractionDebug = true;
            EditorUtility.SetDirty(interaction);
        }

        PlayerMischiefAction mischief = player.GetComponent<PlayerMischiefAction>();
        if (mischief != null)
        {
            mischief.logMischiefDebug = true;
            EditorUtility.SetDirty(mischief);
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (capsule != null && rb != null)
        {
            float halfHeight = capsule.height * 0.5f;
            Vector3 pos = player.transform.position;
            pos.y = halfHeight;
            player.transform.position = pos;
        }
    }

    private static void FixInteractable(string objectName, Vector3 zoneCenter, Vector3 zoneSize)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null)
        {
            Debug.LogWarning($"未找到 {objectName}。");
            return;
        }

        InteractableBodySetup setup = root.GetComponent<InteractableBodySetup>();
        if (setup == null)
            setup = root.AddComponent<InteractableBodySetup>();

        setup.zoneLocalCenter = zoneCenter;
        setup.zoneSize = zoneSize;
        setup.Apply();
    }
}
#endif
