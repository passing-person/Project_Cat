#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainScenePlayableBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string RootName = "MainScenePlayableRoot";
    private const string CoreAssetFolder = "Assets/ScriptableObjects/01_Core";
    private const string DefaultStagePath = CoreAssetFolder + "/DefaultStageData.asset";
    private const string KeyboardDataPath = CoreAssetFolder + "/KeyboardTargetData.asset";
    private const string PhoneDataPath = CoreAssetFolder + "/PhoneTargetData.asset";
    private const string WaterDataPath = CoreAssetFolder + "/WaterDispenserTargetData.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/02_Player/PlayerCat.prefab";
    private const string SupervisorPrefabPath = "Assets/Prefabs/03_NPCAI/Final NPCs/Supervisor.prefab";

    [MenuItem("Tools/Project Cat/MainScene/Build Playable MainScene")]
    public static void BuildPlayableMainScene()
    {
        EnsureSceneOpen();
        EnsureAssetFolder(CoreAssetFolder);

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        StageData stageData = CreateOrLoadDefaultStageData();
        MischiefTargetData keyboardData = CreateOrLoadTargetData(KeyboardDataPath, "Keyboard", MischiefType.Press, 10f, 8f, "Supervisor");
        MischiefTargetData phoneData = CreateOrLoadTargetData(PhoneDataPath, "Phone", MischiefType.Press, 15f, 8f, "Supervisor");
        MischiefTargetData waterData = CreateOrLoadTargetData(WaterDataPath, "WaterDispenser", MischiefType.Push, 15f, 8f, "Supervisor");

        GameObject root = CreateEmpty(RootName, null, Vector3.zero);
        CreateEnvironment(root.transform);
        GameObject systems = CreateSystems(root.transform, stageData);
        GameObject player = CreatePlayer(root.transform);
        CreateMischiefTarget("Keyboard", root.transform, keyboardData, new Vector3(0f, 0.92f, 2f), new Vector3(0.7f, 0.08f, 0.25f));
        CreateMischiefTarget("Phone", root.transform, phoneData, new Vector3(1.1f, 0.92f, 2f), new Vector3(0.25f, 0.12f, 0.25f));
        CreateMischiefTarget("WaterDispenser", root.transform, waterData, new Vector3(-2.8f, 0.7f, 1.6f), new Vector3(0.45f, 1.4f, 0.45f));
        CreateHideSpot(root.transform, new Vector3(-2.4f, 0.35f, -1.2f));
        CreateSupervisorOrSpawnPoint(root.transform);
        SetupPlayerCamera(player);
        WirePlayer(player);
        BuildNavMeshIfPackageExists(root);

        CoreReferenceValidator validator = systems.GetComponent<CoreReferenceValidator>();
        if (validator != null)
        {
            validator.LogValidation();
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Playable MainScene generated. Open Assets/Scenes/MainScene.unity and press Play.");
    }

    private static void EnsureSceneOpen()
    {
        if (SceneManager.GetActiveScene().path == MainScenePath)
        {
            return;
        }

        if (File.Exists(MainScenePath))
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            return;
        }

        EnsureAssetFolder("Assets/Scenes");
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, MainScenePath);
    }

    private static void CreateEnvironment(Transform parent)
    {
        GameObject env = CreateEmpty("Environment", parent, Vector3.zero);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(floor, "Create MVP Floor");
        floor.name = "MVP_Floor";
        floor.transform.SetParent(env.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
        floor.isStatic = true;

        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(desk, "Create Supervisor Desk");
        desk.name = "SupervisorDesk";
        desk.transform.SetParent(env.transform, false);
        desk.transform.localPosition = new Vector3(0.3f, 0.45f, 2f);
        desk.transform.localScale = new Vector3(2.4f, 0.8f, 1f);
        desk.isStatic = true;

        CreateWall(env.transform, "BackWall", new Vector3(0f, 1f, 4f), new Vector3(8f, 2f, 0.15f));
        CreateWall(env.transform, "LeftWall", new Vector3(-4f, 1f, 0f), new Vector3(0.15f, 2f, 8f));
        CreateWall(env.transform, "RightWall", new Vector3(4f, 1f, 0f), new Vector3(0.15f, 2f, 8f));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(wall, "Create Wall");
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.isStatic = true;
    }

    private static GameObject CreateSystems(Transform parent, StageData stageData)
    {
        GameObject systems = CreateEmpty("Systems", parent, Vector3.zero);

        UIManager uiManager = systems.AddComponent<UIManager>();
        AudioManager audioManager = systems.AddComponent<AudioManager>();
        SimpleFeedbackAudio feedbackAudio = systems.AddComponent<SimpleFeedbackAudio>();
        NavMeshAgentPlacementFixer navMeshFixer = systems.AddComponent<NavMeshAgentPlacementFixer>();
        AudioSource sfxSource = systems.AddComponent<AudioSource>();
        AudioSource bgmSource = systems.AddComponent<AudioSource>();
        GameManager gameManager = systems.AddComponent<GameManager>();
        StageManager stageManager = systems.AddComponent<StageManager>();
        ScoreManager scoreManager = systems.AddComponent<ScoreManager>();
        RageManager rageManager = systems.AddComponent<RageManager>();
        ObjectiveManager objectiveManager = systems.AddComponent<ObjectiveManager>();
        FailManager failManager = systems.AddComponent<FailManager>();
        HidingManager hidingManager = systems.AddComponent<HidingManager>();
        MischiefManager mischiefManager = systems.AddComponent<MischiefManager>();
        CoreFacade coreFacade = systems.AddComponent<CoreFacade>();
        CoreReferenceValidator validator = systems.AddComponent<CoreReferenceValidator>();
        MainSceneStarter starter = systems.AddComponent<MainSceneStarter>();

        gameManager.stageManager = stageManager;
        gameManager.uiBridgeBehaviour = uiManager;

        stageManager.gameManager = gameManager;
        stageManager.scoreManager = scoreManager;
        stageManager.objectiveManager = objectiveManager;
        stageManager.failManager = failManager;
        stageManager.uiBridgeBehaviour = uiManager;
        stageManager.defaultStageData = stageData;

        scoreManager.uiBridgeBehaviour = uiManager;
        scoreManager.autoTick = true;

        rageManager.scoreManager = scoreManager;
        rageManager.uiBridgeBehaviour = uiManager;

        objectiveManager.scoreManager = scoreManager;
        objectiveManager.stageManager = stageManager;
        objectiveManager.uiBridgeBehaviour = uiManager;

        failManager.stageManager = stageManager;
        failManager.objectiveManager = objectiveManager;
        failManager.caughtRule = stageData.caughtRule;

        hidingManager.scoreManager = scoreManager;
        hidingManager.uiBridgeBehaviour = uiManager;
        hidingManager.autoTick = true;
        hidingManager.ConfigureFromStageData(stageData);

        mischiefManager.rageManager = rageManager;
        mischiefManager.scoreManager = scoreManager;
        mischiefManager.objectiveManager = objectiveManager;
        mischiefManager.uiBridgeBehaviour = uiManager;
        mischiefManager.autoTickTargetCooldowns = true;

        coreFacade.gameManager = gameManager;
        coreFacade.stageManager = stageManager;
        coreFacade.mischiefManager = mischiefManager;
        coreFacade.scoreManager = scoreManager;
        coreFacade.rageManager = rageManager;
        coreFacade.objectiveManager = objectiveManager;
        coreFacade.failManager = failManager;
        coreFacade.hidingManager = hidingManager;
        coreFacade.uiBridgeBehaviour = uiManager;
        coreFacade.cuteActionRadius = 4f;
        coreFacade.cuteActionRageReduction = 20f;
        coreFacade.defaultSecurityMultiplier = stageData.securityMultiplierOverride;
        coreFacade.autoResolveReferences = false;
        coreFacade.autoWireReferences = false;

        validator.coreFacade = coreFacade;
        validator.gameManager = gameManager;
        validator.stageManager = stageManager;
        validator.mischiefManager = mischiefManager;
        validator.scoreManager = scoreManager;
        validator.rageManager = rageManager;
        validator.objectiveManager = objectiveManager;
        validator.failManager = failManager;
        validator.hidingManager = hidingManager;
        validator.uiBridgeBehaviour = uiManager;
        validator.logValidationOnStart = true;

        SetPrivateField(audioManager, "sfxSource", sfxSource);
        SetPrivateField(audioManager, "bgmSource", bgmSource);
        SetPrivateField(uiManager, "coreFacade", coreFacade);
        SetPrivateField(uiManager, "feedbackAudio", feedbackAudio);
        SetPrivateField(starter, "coreFacade", coreFacade);
        SetPrivateField(starter, "uiManager", uiManager);
        SetPrivateField(starter, "stageData", stageData);

        return systems;
    }

    private static GameObject CreatePlayer(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject player;

        if (prefab != null)
        {
            player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(player, "Create PlayerCat from prefab");
            player.name = "PlayerCat";
            player.transform.SetParent(parent, false);
            player.transform.localPosition = new Vector3(0f, 0.6f, -1.5f);
            EnsurePlayerCoreComponents(player);
            return player;
        }

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(player, "Create PlayerCat fallback");
        player.name = "PlayerCat";
        player.transform.SetParent(parent, false);
        player.transform.localPosition = new Vector3(0f, 0.6f, -1.5f);
        player.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        EnsurePlayerCoreComponents(player);
        return player;
    }

    private static void EnsurePlayerCoreComponents(GameObject player)
    {
        EnsureComponent<PlayerController>(player);
        EnsureComponent<PlayerAnimationController>(player);
        EnsureComponent<PlayerSfxController>(player);
        EnsureComponent<PlayerInteraction>(player);
        EnsureComponent<PlayerMischiefAction>(player);
        EnsureComponent<PlayerCuteAction>(player);
        EnsureComponent<PlayerHide>(player);
        EnsureComponent<PlayerMovement>(player);
        EnsureComponent<PlayerBootstrap>(player);

        if (player.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (player.GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1f;
            capsule.radius = 0.25f;
        }

        Transform groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject groundCheckObject = CreateEmpty("GroundCheck", player.transform, new Vector3(0f, -0.55f, 0f));
            groundCheck = groundCheckObject.transform;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement.groundLayer = LayerMask.GetMask("Default");
        SetPrivateField(movement, "groundCheck", groundCheck);
    }

    private static void SetupPlayerCamera(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        Camera existingMainCamera = Camera.main;
        GameObject cameraObject = existingMainCamera != null ? existingMainCamera.gameObject : new GameObject("PlayerCamera");
        if (existingMainCamera == null)
        {
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Player Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        cameraObject.name = "PlayerCamera";
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform.parent, false);
        cameraObject.transform.localPosition = player.transform.localPosition + new Vector3(0f, 1.2f, -3.2f);
        cameraObject.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

        FirstPersonCameraLook oldLook = player.GetComponent<FirstPersonCameraLook>();
        if (oldLook != null)
        {
            oldLook.enabled = false;
        }

        CameraManager oldCameraManager = cameraObject.GetComponent<CameraManager>();
        if (oldCameraManager != null)
        {
            oldCameraManager.enabled = false;
        }

        ThirdPersonCameraController thirdPersonCamera = EnsureComponent<ThirdPersonCameraController>(cameraObject);
        SetPrivateField(thirdPersonCamera, "target", player.transform);
        SetPrivateField(thirdPersonCamera, "playerController", player.GetComponent<PlayerController>());

        UIManager uiManager = Object.FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            SetPrivateField(uiManager, "cameraController", thirdPersonCamera);
        }
    }

    private static void WirePlayer(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        CoreFacade coreFacade = Object.FindObjectOfType<CoreFacade>();
        MischiefManager mischiefManager = Object.FindObjectOfType<MischiefManager>();
        RageManager rageManager = Object.FindObjectOfType<RageManager>();
        HidingManager hidingManager = Object.FindObjectOfType<HidingManager>();
        UIManager uiManager = Object.FindObjectOfType<UIManager>();
        AudioManager audioManager = Object.FindObjectOfType<AudioManager>();
        PlayerController controller = player.GetComponent<PlayerController>();
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        PlayerAnimationController animationController = player.GetComponent<PlayerAnimationController>();
        PlayerSfxController sfxController = player.GetComponent<PlayerSfxController>();

        SetPrivateField(interaction, "playerController", controller);
        SetPrivateField(interaction, "uiManager", uiManager);
        interaction.interactionRange = 0.75f;
        interaction.detectionRadius = 1.8f;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.moveSpeed = 2.6f;
            movement.sprintMultiplier = 1.55f;
        }

        PlayerMischiefAction mischief = player.GetComponent<PlayerMischiefAction>();
        SetPrivateField(mischief, "playerController", controller);
        SetPrivateField(mischief, "playerInteraction", interaction);
        SetPrivateField(mischief, "coreFacade", coreFacade);
        SetPrivateField(mischief, "mischiefManager", mischiefManager);
        SetPrivateField(mischief, "animationController", animationController);
        SetPrivateField(mischief, "sfxController", sfxController);
        SetPrivateField(mischief, "uiManager", uiManager);

        PlayerCuteAction cute = player.GetComponent<PlayerCuteAction>();
        SetPrivateField(cute, "coreFacade", coreFacade);
        SetPrivateField(cute, "rageManager", rageManager);
        SetPrivateField(cute, "uiManager", uiManager);
        SetPrivateField(cute, "animationController", animationController);
        SetPrivateField(cute, "sfxController", sfxController);
        cute.radius = 4f;
        cute.rageReduction = 20f;
        cute.cooldown = 20f;

        PlayerHide hide = player.GetComponent<PlayerHide>();
        SetPrivateField(hide, "playerController", controller);
        SetPrivateField(hide, "playerInteraction", interaction);
        SetPrivateField(hide, "coreFacade", coreFacade);
        SetPrivateField(hide, "hidingManager", hidingManager);
        SetPrivateField(hide, "uiManager", uiManager);
        SetPrivateField(hide, "animationController", animationController);
        SetPrivateField(hide, "sfxController", sfxController);

        SetPrivateField(sfxController, "audioManager", audioManager);
    }

    private static void CreateMischiefTarget(string name, Transform parent, MischiefTargetData data, Vector3 position, Vector3 scale)
    {
        GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(targetObject, "Create Mischief Target");
        targetObject.name = name;
        targetObject.transform.SetParent(parent, false);
        targetObject.transform.localPosition = position;
        targetObject.transform.localScale = scale;

        MischiefTarget target = targetObject.AddComponent<MischiefTarget>();
        targetObject.AddComponent<InteractableHighlighter>();
        SetPrivateField(target, "data", data);
    }

    private static void CreateHideSpot(Transform parent, Vector3 position)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(box, "Create Hide Spot");
        box.name = "HideSpot_Box";
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);

        HideSpot hideSpot = box.AddComponent<HideSpot>();
        box.AddComponent<InteractableHighlighter>();

        GameObject hidePoint = CreateEmpty("HidePoint", box.transform, new Vector3(0f, 0.2f, 0f));
        SetPrivateField(hideSpot, "interactionId", "HideSpot_Box");
        SetPrivateField(hideSpot, "hidePoint", hidePoint.transform);
        SetPrivateField(hideSpot, "coreFacade", Object.FindObjectOfType<CoreFacade>());
        SetPrivateField(hideSpot, "hidingManager", Object.FindObjectOfType<HidingManager>());
    }

    private static void CreateSupervisorOrSpawnPoint(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SupervisorPrefabPath);
        if (prefab != null)
        {
            GameObject supervisor = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(supervisor, "Create Supervisor NPC");
            supervisor.name = "Supervisor";
            supervisor.transform.SetParent(parent, false);
            supervisor.transform.localPosition = new Vector3(0f, 0f, 3.2f);
            supervisor.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return;
        }

        GameObject spawnPoint = CreateEmpty("NPCSpawn_Supervisor", parent, new Vector3(0f, 0f, 3.2f));
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(marker, "Create Supervisor Placeholder");
        marker.name = "SupervisorPlaceholder_NoNpcPrefab";
        marker.transform.SetParent(spawnPoint.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        marker.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
    }


    private static void BuildNavMeshIfPackageExists(GameObject root)
    {
        System.Type surfaceType = FindTypeByName("Unity.AI.Navigation.NavMeshSurface");
        if (surfaceType == null)
        {
            Debug.LogWarning("AI Navigation package NavMeshSurface type was not found. NPC NavMesh may need manual baking.");
            return;
        }

        GameObject surfaceObject = GameObject.Find("MVP_NavMeshSurface");
        if (surfaceObject == null)
        {
            surfaceObject = CreateEmpty("MVP_NavMeshSurface", root.transform, Vector3.zero);
        }

        Component surface = surfaceObject.GetComponent(surfaceType);
        if (surface == null)
        {
            surface = surfaceObject.AddComponent(surfaceType);
        }

        MethodInfo buildMethod = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public);
        if (buildMethod == null)
        {
            Debug.LogWarning("NavMeshSurface.BuildNavMesh() was not found. Please bake NavMesh manually.");
            return;
        }

        buildMethod.Invoke(surface, null);
        EditorUtility.SetDirty(surfaceObject);
        Debug.Log("Built NavMesh for MainScene using AI Navigation package.");
    }

    private static System.Type FindTypeByName(string fullName)
    {
        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType(fullName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static StageData CreateOrLoadDefaultStageData()
    {
        StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(DefaultStagePath);
        if (stageData == null)
        {
            stageData = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(stageData, DefaultStagePath);
        }

        stageData.stageId = "MVP_Office";
        stageData.nextStageId = "";
        stageData.objectiveType = ObjectiveType.Custom;
        stageData.targetScore = 300;
        stageData.survivalTime = 0f;
        stageData.caughtRule = CaughtRule.ClearIfEnoughScore;
        stageData.baseScoreRate = 10f;
        stageData.maxScoreMultiplierBonus = 12f;
        stageData.securityMultiplierOverride = 13f;
        stageData.maxHideDuration = 10f;
        stageData.hiddenMultiplierScale = 0.1f;
        stageData.hideSpotUsesPerStage = 1;
        EditorUtility.SetDirty(stageData);
        return stageData;
    }

    private static MischiefTargetData CreateOrLoadTargetData(string path, string targetId, MischiefType type, float rageAmount, float radius, string primaryNpcId)
    {
        MischiefTargetData data = AssetDatabase.LoadAssetAtPath<MischiefTargetData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MischiefTargetData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.targetId = targetId;
        data.mischiefType = type;
        data.instantScoreBonus = 0;
        data.baseRageAmount = rageAmount;
        data.rageRadius = radius;
        data.primaryNpcId = primaryNpcId;
        data.canBeLocked = true;
        data.lockAtRageThreshold = 100f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject CreateEmpty(string name, Transform parent, Vector3 localPosition)
    {
        GameObject gameObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }
        gameObject.transform.localPosition = localPosition;
        return gameObject;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }

    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        if (target == null || value == null)
        {
            return;
        }

        FieldInfo field = null;
        System.Type type = target.GetType();
        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            type = type.BaseType;
        }

        if (field != null)
        {
            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
