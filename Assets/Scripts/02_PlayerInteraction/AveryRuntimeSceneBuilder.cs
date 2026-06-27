using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play 模式下若 Avery 场景尚未搭建，自动创建测试布局。
/// Editor 中打开 Avery.unity 时会通过 AverySceneBuilder 写入场景文件。
/// </summary>
public class AveryRuntimeSceneBuilder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBuildIfNeeded()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Avery")
            return;

        if (GameObject.Find("AveryTestRoot") != null)
            return;

        BuildRuntimeLayout();
    }

    public static void BuildRuntimeLayout()
    {
        GameObject root = new GameObject("AveryTestRoot");

        CreateGround(root.transform);
        CreateDesk(root.transform);
        GameObject systems = CreateSystems(root.transform);
        GameObject player = CreatePlayer(root.transform);
        CreateSupervisor(root.transform, systems, player.transform);
        CreateKeyboard(root.transform);
        CreatePhone(root.transform);
        CreateHideSpot(root.transform);
        PositionCamera(player.transform);

        Debug.Log("Avery 运行时测试布局已创建。");
    }

    private static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
    }

    private static void CreateDesk(Transform parent)
    {
        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = "SupervisorDesk";
        desk.transform.SetParent(parent);
        desk.transform.localPosition = new Vector3(0f, 0.4f, 2f);
        desk.transform.localScale = new Vector3(2f, 0.8f, 1f);
    }

    private static GameObject CreateSystems(Transform parent)
    {
        GameObject systems = new GameObject("Systems");
        systems.transform.SetParent(parent);

        systems.AddComponent<UIManager>();
        systems.AddComponent<AudioManager>();
        GameManager gameManager = systems.AddComponent<GameManager>();
        StageManager stageManager = systems.AddComponent<StageManager>();
        ScoreManager scoreManager = systems.AddComponent<ScoreManager>();
        RageManager rageManager = systems.AddComponent<RageManager>();
        ObjectiveManager objectiveManager = systems.AddComponent<ObjectiveManager>();
        FailManager failManager = systems.AddComponent<FailManager>();
        HidingManager hidingManager = systems.AddComponent<HidingManager>();
        MischiefManager mischiefManager = systems.AddComponent<MischiefManager>();
        CoreFacade coreFacade = systems.AddComponent<CoreFacade>();
        systems.AddComponent<AverySceneStarter>();

        gameManager.stageManager = stageManager;
        stageManager.gameManager = gameManager;
        stageManager.scoreManager = scoreManager;
        stageManager.objectiveManager = objectiveManager;
        stageManager.failManager = failManager;

        StageData runtimeStage = ScriptableObject.CreateInstance<StageData>();
        runtimeStage.stageId = "AveryTutorial";
        runtimeStage.targetScore = 300;
        runtimeStage.baseScoreRate = 10f;
        runtimeStage.maxScoreMultiplierBonus = 12f;
        runtimeStage.maxHideDuration = 10f;
        runtimeStage.hiddenMultiplierScale = 0.1f;
        runtimeStage.caughtRule = CaughtRule.ClearIfEnoughScore;
        stageManager.defaultStageData = runtimeStage;

        rageManager.scoreManager = scoreManager;
        objectiveManager.scoreManager = scoreManager;
        objectiveManager.stageManager = stageManager;
        failManager.stageManager = stageManager;
        failManager.objectiveManager = objectiveManager;
        failManager.caughtRule = CaughtRule.ClearIfEnoughScore;

        mischiefManager.rageManager = rageManager;
        mischiefManager.scoreManager = scoreManager;
        mischiefManager.objectiveManager = objectiveManager;

        hidingManager.scoreManager = scoreManager;
        hidingManager.autoTick = true;
        hidingManager.maxHideDuration = 10f;
        hidingManager.hiddenMultiplierScale = 0.1f;

        coreFacade.gameManager = gameManager;
        coreFacade.stageManager = stageManager;
        coreFacade.mischiefManager = mischiefManager;
        coreFacade.scoreManager = scoreManager;
        coreFacade.rageManager = rageManager;
        coreFacade.objectiveManager = objectiveManager;
        coreFacade.failManager = failManager;
        coreFacade.hidingManager = hidingManager;
        coreFacade.cuteActionRadius = 4f;
        coreFacade.cuteActionRageReduction = 20f;
        coreFacade.autoResolveReferences = false;
        coreFacade.autoWireReferences = false;

        return systems;
    }

    private static GameObject CreatePlayer(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "PlayerCat";
        player.transform.SetParent(parent);
        player.transform.localPosition = new Vector3(0f, 1f, 0f);

        Object.Destroy(player.GetComponent<CapsuleCollider>());

        CapsuleCollider body = player.AddComponent<CapsuleCollider>();
        body.height = 1f;
        body.radius = 0.25f;

        SphereCollider trigger = player.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.35f;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerMischiefAction>();
        player.AddComponent<PlayerCuteAction>();
        player.AddComponent<PlayerHide>();
        player.AddComponent<PlayerAnimationController>();
        player.AddComponent<PlayerSfxController>();
        player.AddComponent<PlayerBootstrap>();

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.51f, 0f);

        return player;
    }

    private static void CreateSupervisor(Transform parent, GameObject systems, Transform playerTransform)
    {
        GameObject supervisor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        supervisor.name = "Supervisor";
        supervisor.transform.SetParent(parent);
        supervisor.transform.localPosition = new Vector3(0f, 1f, 4f);

        NpcController controller = supervisor.AddComponent<NpcController>();
        NpcChase chase = supervisor.AddComponent<NpcChase>();
        supervisor.AddComponent<NpcPerception>();
        NpcCatch npcCatch = supervisor.AddComponent<NpcCatch>();

        SphereCollider catchTrigger = supervisor.AddComponent<SphereCollider>();
        catchTrigger.isTrigger = true;
        catchTrigger.radius = 0.6f;

        RageManager rageManager = systems.GetComponent<RageManager>();
        FailManager failManager = systems.GetComponent<FailManager>();

        NpcData npcData = ScriptableObject.CreateInstance<NpcData>();
        npcData.npcId = "Supervisor";
        npcData.npcType = NpcType.Supervisor;

        SetPrivateField(controller, "npcData", npcData);
        SetPrivateField(controller, "rageManager", rageManager);
        SetPrivateField(controller, "npcChase", chase);
        SetPrivateField(npcCatch, "npcController", controller);
        SetPrivateField(npcCatch, "failManager", failManager);

        chase.SetTarget(playerTransform);
    }

    private static void CreateKeyboard(Transform parent)
    {
        GameObject keyboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        keyboard.name = "Keyboard";
        keyboard.transform.SetParent(parent);
        keyboard.transform.localPosition = new Vector3(0f, 0.86f, 2f);
        keyboard.transform.localScale = new Vector3(0.6f, 0.05f, 0.2f);

        BoxCollider trigger = keyboard.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.5f, 3f, 2f);

        MischiefTarget target = keyboard.AddComponent<MischiefTarget>();
        SetPrivateField(target, "interactionId", "Keyboard");
        SetPrivateField(target, "baseRageAmount", 10f);
        SetPrivateField(target, "rageRadius", 8f);
        SetPrivateField(target, "primaryNpcId", "Supervisor");
    }

    private static void CreatePhone(Transform parent)
    {
        GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        phone.name = "Phone";
        phone.transform.SetParent(parent);
        phone.transform.localPosition = new Vector3(0.8f, 0.9f, 2f);
        phone.transform.localScale = new Vector3(0.15f, 0.08f, 0.15f);

        BoxCollider trigger = phone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3f, 3f, 3f);

        MischiefTarget target = phone.AddComponent<MischiefTarget>();
        SetPrivateField(target, "interactionId", "Phone");
        SetPrivateField(target, "baseRageAmount", 10f);
        SetPrivateField(target, "rageRadius", 8f);
        SetPrivateField(target, "primaryNpcId", "Supervisor");
    }

    private static void CreateHideSpot(Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "HideSpot_Box";
        box.transform.SetParent(parent);
        box.transform.localPosition = new Vector3(-2f, 0.25f, 0f);
        box.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

        BoxCollider trigger = box.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2f, 2f, 2f);

        HideSpot hideSpot = box.AddComponent<HideSpot>();
        GameObject hidePoint = new GameObject("HidePoint");
        hidePoint.transform.SetParent(box.transform);
        hidePoint.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        SetPrivateField(hideSpot, "interactionId", "HideSpot_Box");
        SetPrivateField(hideSpot, "hidePoint", hidePoint.transform);
    }

    private static void PositionCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.transform.position = player.position + new Vector3(0f, 4f, -6f);
        camera.transform.LookAt(player.position + Vector3.up);
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value) where T : Object
    {
        if (target == null)
            return;

        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
