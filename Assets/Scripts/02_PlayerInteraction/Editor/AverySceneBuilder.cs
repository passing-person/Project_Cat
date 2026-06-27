#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AverySceneAutoBuildHook
{
    static AverySceneAutoBuildHook()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryBuildActiveScene;
        UnityEditor.Compilation.CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += TryBuildActiveScene;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path != AverySceneBuilder.ScenePath)
            return;

        TryBuildActiveScene();
    }

    private static void TryBuildActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != AverySceneBuilder.ScenePath)
            return;

        if (GameObject.Find(AverySceneBuilder.RootName) != null)
            return;

        AverySceneBuilder.BuildAndSave();
    }
}

public static class AverySceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Avery.unity";
    public const string RootName = "AveryTestRoot";
    private const string AssetFolder = "Assets/Scenes/AveryTestAssets";

    [MenuItem("Tools/Project Cat/Build Avery Test Scene")]
    public static void BuildFromMenu()
    {
        BuildAndSave();
    }

    public static void BuildAndSave()
    {
        EnsureAssetFolder();
        OpenTargetScene();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
            Undo.DestroyObjectImmediate(oldRoot);

        StageData stageData = CreateOrLoadStageData();
        NpcData supervisorData = CreateOrLoadSupervisorData();
        MischiefTargetData keyboardData = CreateOrLoadKeyboardData();
        MischiefTargetData phoneData = CreateOrLoadPhoneData();

        GameObject root = CreateGameObject(RootName, null, Vector3.zero);

        CreateEnvironment(root.transform);
        GameObject systems = CreateSystems(root.transform, stageData);
        GameObject player = CreatePlayerCat(root.transform);
        CreateSupervisor(root.transform, supervisorData, systems);
        CreateKeyboard(root.transform, keyboardData);
        CreatePhone(root.transform, phoneData);
        CreateHideSpot(root.transform);
        SetupCamera(player.transform);

        CoreFacade coreFacade = systems.GetComponentInChildren<CoreFacade>();
        WirePlayer(player, systems, coreFacade);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("Avery 测试场景搭建完成。打开 Assets/Scenes/Avery.unity 后按 Play 即可测试。");
    }

    private static void OpenTargetScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void CreateEnvironment(Transform parent)
    {
        GameObject env = CreateGameObject("Environment", parent, Vector3.zero);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        ground.name = "Ground";
        ground.transform.SetParent(env.transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        ground.isStatic = true;

        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(desk, "Create Desk");
        desk.name = "SupervisorDesk";
        desk.transform.SetParent(env.transform);
        desk.transform.localPosition = new Vector3(0f, 0.4f, 2f);
        desk.transform.localScale = new Vector3(2f, 0.8f, 1f);
        desk.isStatic = true;
    }

    private static GameObject CreateSystems(Transform parent, StageData stageData)
    {
        GameObject systems = CreateGameObject("Systems", parent, Vector3.zero);

        UIManager uiManager = CreateComponentObject<UIManager>("UIManager", systems.transform, Vector3.zero);
        AudioManager audioManager = CreateComponentObject<AudioManager>("AudioManager", systems.transform, Vector3.zero);
        GameManager gameManager = CreateComponentObject<GameManager>("GameManager", systems.transform, Vector3.zero);
        StageManager stageManager = CreateComponentObject<StageManager>("StageManager", systems.transform, Vector3.zero);
        ScoreManager scoreManager = CreateComponentObject<ScoreManager>("ScoreManager", systems.transform, Vector3.zero);
        RageManager rageManager = CreateComponentObject<RageManager>("RageManager", systems.transform, Vector3.zero);
        ObjectiveManager objectiveManager = CreateComponentObject<ObjectiveManager>("ObjectiveManager", systems.transform, Vector3.zero);
        FailManager failManager = CreateComponentObject<FailManager>("FailManager", systems.transform, Vector3.zero);
        HidingManager hidingManager = CreateComponentObject<HidingManager>("HidingManager", systems.transform, Vector3.zero);
        MischiefManager mischiefManager = CreateComponentObject<MischiefManager>("MischiefManager", systems.transform, Vector3.zero);
        CoreFacade coreFacade = CreateComponentObject<CoreFacade>("CoreFacade", systems.transform, Vector3.zero);
        AverySceneStarter sceneStarter = CreateComponentObject<AverySceneStarter>("AverySceneStarter", systems.transform, Vector3.zero);

        gameManager.stageManager = stageManager;

        stageManager.gameManager = gameManager;
        stageManager.scoreManager = scoreManager;
        stageManager.objectiveManager = objectiveManager;
        stageManager.failManager = failManager;
        stageManager.defaultStageData = stageData;

        scoreManager.autoTick = true;

        rageManager.scoreManager = scoreManager;

        objectiveManager.scoreManager = scoreManager;
        objectiveManager.stageManager = stageManager;

        failManager.stageManager = stageManager;
        failManager.objectiveManager = objectiveManager;
        failManager.caughtRule = stageData.caughtRule;

        mischiefManager.rageManager = rageManager;
        mischiefManager.scoreManager = scoreManager;
        mischiefManager.objectiveManager = objectiveManager;
        mischiefManager.autoTickTargetCooldowns = true;

        hidingManager.scoreManager = scoreManager;
        hidingManager.autoTick = true;
        hidingManager.ConfigureFromStageData(stageData);

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
        coreFacade.defaultSecurityMultiplier = stageData.securityMultiplierOverride;
        coreFacade.autoResolveReferences = false;
        coreFacade.autoWireReferences = false;

        SetReference(sceneStarter, "gameManager", gameManager);

        SetReference(audioManager, "sfxSource", audioManager.gameObject.AddComponent<AudioSource>());
        SetReference(audioManager, "bgmSource", audioManager.gameObject.AddComponent<AudioSource>());

        return systems;
    }

    private static GameObject CreatePlayerCat(Transform parent)
    {
        GameObject player = CreateGameObject("PlayerCat", parent, Vector3.zero);

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerMischiefAction>();
        player.AddComponent<PlayerCuteAction>();
        player.AddComponent<PlayerHide>();
        player.AddComponent<PlayerAnimationController>();
        player.AddComponent<PlayerSfxController>();
        player.AddComponent<PlayerBootstrap>();

        CreateGameObject("GroundCheck", player.transform, Vector3.zero);

        PlayerBodySetup bodySetup = player.AddComponent<PlayerBodySetup>();
        bodySetup.modelScale = 0.35f;
        bodySetup.Apply();

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
            player.transform.localPosition = new Vector3(0f, capsule.height * 0.5f, 0f);

        SetReference(player.GetComponent<PlayerMovement>(), "groundCheck", player.transform.Find("GroundCheck"));
        SerializedObject movementObject = new SerializedObject(player.GetComponent<PlayerMovement>());
        movementObject.FindProperty("groundLayer").intValue = LayerMask.GetMask("Default");
        movementObject.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static void CreateSupervisor(Transform parent, NpcData npcData, GameObject systems)
    {
        GameObject supervisor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(supervisor, "Create Supervisor");
        supervisor.name = "Supervisor";
        supervisor.transform.SetParent(parent);
        supervisor.transform.localPosition = new Vector3(0f, 1f, 4f);
        supervisor.transform.localScale = new Vector3(1f, 1.2f, 1f);

        NpcController controller = supervisor.AddComponent<NpcController>();
        NpcChase chase = supervisor.AddComponent<NpcChase>();
        NpcPerception perception = supervisor.AddComponent<NpcPerception>();
        NpcCatch npcCatch = supervisor.AddComponent<NpcCatch>();

        SphereCollider catchTrigger = supervisor.AddComponent<SphereCollider>();
        catchTrigger.isTrigger = true;
        catchTrigger.radius = 0.6f;
        catchTrigger.center = Vector3.zero;

        RageManager rageManager = systems.GetComponentInChildren<RageManager>();
        FailManager failManager = systems.GetComponentInChildren<FailManager>();

        SetReference(controller, "npcData", npcData);
        SetReference(controller, "rageManager", rageManager);
        SetReference(controller, "npcChase", chase);
        SetReference(npcCatch, "npcController", controller);
        SetReference(npcCatch, "failManager", failManager);
        SetReference(chase, "npcController", controller);
    }

    private static void CreateKeyboard(Transform parent, MischiefTargetData data)
    {
        GameObject keyboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(keyboard, "Create Keyboard");
        keyboard.name = "Keyboard";
        keyboard.transform.SetParent(parent);
        keyboard.transform.localPosition = new Vector3(0f, 0.86f, 2f);
        keyboard.transform.localScale = new Vector3(0.6f, 0.05f, 0.2f);

        Object.DestroyImmediate(keyboard.GetComponent<BoxCollider>());

        MischiefTarget target = keyboard.AddComponent<MischiefTarget>();
        InteractableHighlighter highlighter = keyboard.AddComponent<InteractableHighlighter>();

        GameObject highlight = CreateGameObject("Highlight", keyboard.transform, Vector3.zero);
        highlight.transform.localScale = new Vector3(1.2f, 2f, 1.5f);
        Object.DestroyImmediate(highlight.GetComponent<BoxCollider>());
        Renderer renderer = highlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        }
        highlight.SetActive(false);

        SetReference(target, "data", data);
        SetReference(highlighter, "highlightObject", highlight);

        ConfigureInteractableBody(keyboard, new Vector3(0f, 10f, 0f), new Vector3(1.4f, 1.4f, 1.4f));
    }

    private static void CreatePhone(Transform parent, MischiefTargetData data)
    {
        GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(phone, "Create Phone");
        phone.name = "Phone";
        phone.transform.SetParent(parent);
        phone.transform.localPosition = new Vector3(0.8f, 0.9f, 2f);
        phone.transform.localScale = new Vector3(0.15f, 0.08f, 0.15f);

        Object.DestroyImmediate(phone.GetComponent<BoxCollider>());

        MischiefTarget target = phone.AddComponent<MischiefTarget>();
        SetReference(target, "data", data);

        ConfigureInteractableBody(phone, new Vector3(0f, 8f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
    }

    private static void CreateHideSpot(Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(box, "Create HideSpot");
        box.name = "HideSpot_Box";
        box.transform.SetParent(parent);
        box.transform.localPosition = new Vector3(-2f, 0.25f, 0f);
        box.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

        Object.DestroyImmediate(box.GetComponent<BoxCollider>());

        HideSpot hideSpot = box.AddComponent<HideSpot>();
        GameObject hidePoint = CreateGameObject("HidePoint", box.transform, new Vector3(0f, 0.15f, 0f));

        SerializedObject hideSpotObject = new SerializedObject(hideSpot);
        hideSpotObject.FindProperty("interactionId").stringValue = "HideSpot_Box";
        hideSpotObject.FindProperty("hidePoint").objectReferenceValue = hidePoint.transform;
        hideSpotObject.ApplyModifiedPropertiesWithoutUndo();

        ConfigureInteractableBody(box, Vector3.zero, new Vector3(1.4f, 1.2f, 1.4f));
    }

    private static void ConfigureInteractableBody(GameObject owner, Vector3 zoneCenter, Vector3 zoneSize)
    {
        InteractableBodySetup setup = owner.AddComponent<InteractableBodySetup>();
        setup.zoneLocalCenter = zoneCenter;
        setup.zoneSize = zoneSize;
        setup.Apply();
    }

    private static void SetupCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.transform.position = player.position + new Vector3(0f, 4f, -6f);
        camera.transform.LookAt(player.position + Vector3.up);
    }

    private static void WirePlayer(GameObject player, GameObject systems, CoreFacade coreFacade)
    {
        PlayerController controller = player.GetComponent<PlayerController>();
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        PlayerMischiefAction mischief = player.GetComponent<PlayerMischiefAction>();
        PlayerCuteAction cute = player.GetComponent<PlayerCuteAction>();
        PlayerHide hide = player.GetComponent<PlayerHide>();
        PlayerAnimationController animation = player.GetComponent<PlayerAnimationController>();
        PlayerSfxController sfx = player.GetComponent<PlayerSfxController>();

        MischiefManager mischiefManager = systems.GetComponentInChildren<MischiefManager>();
        RageManager rageManager = systems.GetComponentInChildren<RageManager>();
        HidingManager hidingManager = systems.GetComponentInChildren<HidingManager>();
        UIManager uiManager = systems.GetComponentInChildren<UIManager>();
        AudioManager audioManager = systems.GetComponentInChildren<AudioManager>();

        SetReference(movement, "playerController", controller);
        SetReference(movement, "animationController", animation);
        SetReference(movement, "sfxController", sfx);

        SetReference(interaction, "playerController", controller);
        SetReference(interaction, "uiManager", uiManager);

        SetReference(mischief, "playerController", controller);
        SetReference(mischief, "playerInteraction", interaction);
        SetReference(mischief, "mischiefManager", mischiefManager);
        SetReference(mischief, "animationController", animation);
        SetReference(mischief, "sfxController", sfx);

        SetReference(cute, "coreFacade", coreFacade);
        SetReference(cute, "rageManager", rageManager);
        SetReference(cute, "uiManager", uiManager);
        SetReference(cute, "animationController", animation);
        SetReference(cute, "sfxController", sfx);

        SetReference(hide, "playerController", controller);
        SetReference(hide, "playerInteraction", interaction);
        SetReference(hide, "hidingManager", hidingManager);
        SetReference(hide, "coreFacade", coreFacade);
        SetReference(hide, "uiManager", uiManager);
        SetReference(hide, "animationController", animation);
        SetReference(hide, "sfxController", sfx);

        SetReference(controller, "animationController", animation);
        SetReference(controller, "sfxController", sfx);
        SetReference(sfx, "audioManager", audioManager);

        WireSupervisorChaseTarget(player.transform);
    }

    private static void WireSupervisorChaseTarget(Transform playerTransform)
    {
        GameObject supervisor = GameObject.Find("Supervisor");
        if (supervisor == null)
            return;

        NpcChase chase = supervisor.GetComponent<NpcChase>();
        if (chase == null)
            return;

        SetReference(chase, "target", playerTransform);
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        if (target == null)
            return;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"AverySceneBuilder: property '{propertyName}' not found on {target.GetType().Name}");
            return;
        }

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            Debug.LogWarning($"AverySceneBuilder: '{propertyName}' is not an object reference on {target.GetType().Name}");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateGameObject(string name, Transform parent, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        if (parent != null)
            go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        return go;
    }

    private static T CreateComponentObject<T>(string name, Transform parent, Vector3 localPosition) where T : Component
    {
        GameObject go = CreateGameObject(name, parent, localPosition);
        return go.AddComponent<T>();
    }

    private static void EnsureAssetFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Scenes", "AveryTestAssets");
    }

    private static StageData CreateOrLoadStageData()
    {
        string path = Path.Combine(AssetFolder, "StageData_AveryTutorial.asset").Replace("\\", "/");
        StageData data = AssetDatabase.LoadAssetAtPath<StageData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.stageId = "AveryTutorial";
        data.objectiveType = ObjectiveType.ReachScore;
        data.targetScore = 300;
        data.survivalTime = 30f;
        data.baseScoreRate = 10f;
        data.maxScoreMultiplierBonus = 12f;
        data.securityMultiplierOverride = 13f;
        data.maxHideDuration = 0f;
        data.hiddenMultiplierScale = 0.1f;
        data.hideSpotUsesPerStage = 0;
        data.caughtRule = CaughtRule.ClearIfEnoughScore;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static NpcData CreateOrLoadSupervisorData()
    {
        string path = Path.Combine(AssetFolder, "NpcData_Supervisor.asset").Replace("\\", "/");
        NpcData data = AssetDatabase.LoadAssetAtPath<NpcData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<NpcData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.npcId = "Supervisor";
        data.npcType = NpcType.Supervisor;
        data.annoyedThreshold = 40f;
        data.angryThreshold = 70f;
        data.enragedThreshold = 100f;
        data.moveSpeed = 2f;
        data.chaseSpeed = 4f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static MischiefTargetData CreateOrLoadKeyboardData()
    {
        string path = Path.Combine(AssetFolder, "MischiefTargetData_Keyboard.asset").Replace("\\", "/");
        MischiefTargetData data = AssetDatabase.LoadAssetAtPath<MischiefTargetData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MischiefTargetData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.targetId = "Keyboard";
        data.mischiefType = MischiefType.Stomp;
        data.baseRageAmount = 10f;
        data.rageRadius = 8f;
        data.primaryNpcId = "Supervisor";
        data.canBeLocked = true;
        data.lockAtRageThreshold = 50f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static MischiefTargetData CreateOrLoadPhoneData()
    {
        string path = Path.Combine(AssetFolder, "MischiefTargetData_Phone.asset").Replace("\\", "/");
        MischiefTargetData data = AssetDatabase.LoadAssetAtPath<MischiefTargetData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MischiefTargetData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.targetId = "Phone";
        data.mischiefType = MischiefType.Press;
        data.baseRageAmount = 10f;
        data.rageRadius = 8f;
        data.primaryNpcId = "Supervisor";
        EditorUtility.SetDirty(data);
        return data;
    }
}
#endif
