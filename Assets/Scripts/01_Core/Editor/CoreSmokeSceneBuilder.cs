#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CoreSmokeSceneBuilder
{
    private const string RootName = "CoreSmokeTest";
    private const string AssetFolder = "Assets/Scripts/01_Core/Tests/GeneratedCoreTestAssets";

    [MenuItem("Tools/Project Cat/Core/Build Smoke Test In Current Scene")]
    public static void BuildSmokeTestInCurrentScene()
    {
        EnsureAssetFolder();

        StageData stageData = CreateOrLoadStageData();
        NpcData npcData = CreateOrLoadNpcData();
        MischiefTargetData targetData = CreateOrLoadTargetData();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        GameObject root = CreateGameObject(RootName, null, Vector3.zero);
        GameObject systems = CreateGameObject("Systems", root.transform, Vector3.zero);

        MockUIBridge uiBridge = CreateComponentObject<MockUIBridge>("MockUIBridge", systems.transform, Vector3.zero);

        GameManager gameManager = CreateComponentObject<GameManager>("GameManager", systems.transform, Vector3.zero);
        StageManager stageManager = CreateComponentObject<StageManager>("StageManager", systems.transform, Vector3.zero);
        ScoreManager scoreManager = CreateComponentObject<ScoreManager>("ScoreManager", systems.transform, Vector3.zero);
        RageManager rageManager = CreateComponentObject<RageManager>("RageManager", systems.transform, Vector3.zero);
        ObjectiveManager objectiveManager = CreateComponentObject<ObjectiveManager>("ObjectiveManager", systems.transform, Vector3.zero);
        FailManager failManager = CreateComponentObject<FailManager>("FailManager", systems.transform, Vector3.zero);
        MischiefManager mischiefManager = CreateComponentObject<MischiefManager>("MischiefManager", systems.transform, Vector3.zero);
        CoreFacade coreFacade = CreateComponentObject<CoreFacade>("CoreFacade", systems.transform, Vector3.zero);
        CoreReferenceValidator referenceValidator = CreateComponentObject<CoreReferenceValidator>("CoreReferenceValidator", systems.transform, Vector3.zero);

        GameObject supervisorObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(supervisorObject, "Create Mock Supervisor");
        supervisorObject.name = "MockSupervisor";
        supervisorObject.transform.SetParent(root.transform);
        supervisorObject.transform.position = new Vector3(0f, 0.5f, 0f);
        MockRageReceiver supervisor = supervisorObject.AddComponent<MockRageReceiver>();
        supervisor.npcId = "Supervisor";
        supervisor.npcData = npcData;

        GameObject keyboardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(keyboardObject, "Create Mock Keyboard");
        keyboardObject.name = "MockKeyboardMischiefSource";
        keyboardObject.transform.SetParent(root.transform);
        keyboardObject.transform.position = new Vector3(1.5f, 0.25f, 0f);
        keyboardObject.transform.localScale = new Vector3(1.2f, 0.2f, 0.5f);
        MockMischiefSource keyboardSource = keyboardObject.AddComponent<MockMischiefSource>();
        keyboardSource.targetData = targetData;

        CoreSmokeTestRunner runner = CreateComponentObject<CoreSmokeTestRunner>("CoreSmokeTestRunner", root.transform, new Vector3(0f, 0f, 2f));

        WireManagers(
            gameManager,
            stageManager,
            scoreManager,
            rageManager,
            objectiveManager,
            failManager,
            mischiefManager,
            coreFacade,
            referenceValidator,
            uiBridge,
            stageData);

        WireRunner(
            runner,
            gameManager,
            stageManager,
            mischiefManager,
            scoreManager,
            rageManager,
            objectiveManager,
            failManager,
            coreFacade,
            referenceValidator,
            supervisor,
            keyboardSource,
            uiBridge,
            stageData);

        Selection.activeGameObject = root;
        EditorSceneManagerBridge.MarkCurrentSceneDirty();
        Debug.Log("Core smoke test objects were built in the current scene. Press Play to run the test.");
    }

    private static void WireManagers(
        GameManager gameManager,
        StageManager stageManager,
        ScoreManager scoreManager,
        RageManager rageManager,
        ObjectiveManager objectiveManager,
        FailManager failManager,
        MischiefManager mischiefManager,
        CoreFacade coreFacade,
        CoreReferenceValidator referenceValidator,
        MockUIBridge uiBridge,
        StageData stageData)
    {
        gameManager.stageManager = stageManager;
        gameManager.uiBridgeBehaviour = uiBridge;

        stageManager.gameManager = gameManager;
        stageManager.scoreManager = scoreManager;
        stageManager.objectiveManager = objectiveManager;
        stageManager.failManager = failManager;
        stageManager.uiBridgeBehaviour = uiBridge;
        stageManager.defaultStageData = stageData;

        scoreManager.uiBridgeBehaviour = uiBridge;
        scoreManager.autoTick = false;

        rageManager.scoreManager = scoreManager;
        rageManager.uiBridgeBehaviour = uiBridge;

        objectiveManager.scoreManager = scoreManager;
        objectiveManager.stageManager = stageManager;
        objectiveManager.uiBridgeBehaviour = uiBridge;

        failManager.stageManager = stageManager;
        failManager.objectiveManager = objectiveManager;
        failManager.caughtRule = stageData.caughtRule;

        mischiefManager.rageManager = rageManager;
        mischiefManager.scoreManager = scoreManager;
        mischiefManager.objectiveManager = objectiveManager;
        mischiefManager.uiBridgeBehaviour = uiBridge;

        coreFacade.gameManager = gameManager;
        coreFacade.stageManager = stageManager;
        coreFacade.mischiefManager = mischiefManager;
        coreFacade.scoreManager = scoreManager;
        coreFacade.rageManager = rageManager;
        coreFacade.objectiveManager = objectiveManager;
        coreFacade.failManager = failManager;
        coreFacade.uiBridgeBehaviour = uiBridge;
        coreFacade.cuteActionRadius = 5f;
        coreFacade.cuteActionRageReduction = 20f;
        coreFacade.defaultSecurityMultiplier = stageData.securityMultiplierOverride;
        coreFacade.autoResolveReferences = false;
        coreFacade.autoWireReferences = false;

        referenceValidator.coreFacade = coreFacade;
        referenceValidator.gameManager = gameManager;
        referenceValidator.stageManager = stageManager;
        referenceValidator.mischiefManager = mischiefManager;
        referenceValidator.scoreManager = scoreManager;
        referenceValidator.rageManager = rageManager;
        referenceValidator.objectiveManager = objectiveManager;
        referenceValidator.failManager = failManager;
        referenceValidator.uiBridgeBehaviour = uiBridge;
        referenceValidator.autoResolveOnAwake = false;
        referenceValidator.logValidationOnStart = false;
    }

    private static void WireRunner(
        CoreSmokeTestRunner runner,
        GameManager gameManager,
        StageManager stageManager,
        MischiefManager mischiefManager,
        ScoreManager scoreManager,
        RageManager rageManager,
        ObjectiveManager objectiveManager,
        FailManager failManager,
        CoreFacade coreFacade,
        CoreReferenceValidator referenceValidator,
        MockRageReceiver supervisor,
        MockMischiefSource keyboardSource,
        MockUIBridge uiBridge,
        StageData stageData)
    {
        runner.gameManager = gameManager;
        runner.stageManager = stageManager;
        runner.mischiefManager = mischiefManager;
        runner.scoreManager = scoreManager;
        runner.rageManager = rageManager;
        runner.objectiveManager = objectiveManager;
        runner.failManager = failManager;
        runner.coreFacade = coreFacade;
        runner.referenceValidator = referenceValidator;
        runner.supervisor = supervisor;
        runner.keyboardSource = keyboardSource;
        runner.uiBridge = uiBridge;
        runner.stageData = stageData;
        runner.runOnStart = true;
    }

    private static GameObject CreateGameObject(string name, Transform parent, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
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
        if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
        {
            AssetDatabase.CreateFolder("Assets", "Scripts");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scripts/01_Core"))
        {
            AssetDatabase.CreateFolder("Assets/Scripts", "01_Core");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scripts/01_Core/Tests"))
        {
            AssetDatabase.CreateFolder("Assets/Scripts/01_Core", "Tests");
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Scripts/01_Core/Tests", "GeneratedCoreTestAssets");
        }
    }

    private static StageData CreateOrLoadStageData()
    {
        string path = Path.Combine(AssetFolder, "StageData_CoreSmoke.asset").Replace("\\", "/");
        StageData data = AssetDatabase.LoadAssetAtPath<StageData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.stageId = "CoreSmoke";
        data.objectiveType = ObjectiveType.SurviveChase;
        data.targetScore = 100;
        data.survivalTime = 30f;
        data.baseScoreRate = 10f;
        data.maxScoreMultiplierBonus = 12f;
        data.securityMultiplierOverride = 13f;
        data.caughtRule = CaughtRule.AlwaysFail;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static NpcData CreateOrLoadNpcData()
    {
        string path = Path.Combine(AssetFolder, "NpcData_CoreSmokeSupervisor.asset").Replace("\\", "/");
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

    private static MischiefTargetData CreateOrLoadTargetData()
    {
        string path = Path.Combine(AssetFolder, "MischiefTargetData_CoreSmokeKeyboard.asset").Replace("\\", "/");
        MischiefTargetData data = AssetDatabase.LoadAssetAtPath<MischiefTargetData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MischiefTargetData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.targetId = "Keyboard";
        data.mischiefType = MischiefType.Stomp;
        data.instantScoreBonus = 0;
        data.baseRageAmount = 50f;
        data.rageRadius = 5f;
        data.primaryNpcId = "Supervisor";
        data.canBeLocked = true;
        data.lockAtRageThreshold = 70f;
        EditorUtility.SetDirty(data);
        return data;
    }
}

internal static class EditorSceneManagerBridge
{
    public static void MarkCurrentSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
#endif
