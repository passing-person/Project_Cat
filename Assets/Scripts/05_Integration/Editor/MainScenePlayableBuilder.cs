#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class MainScenePlayableBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string RootName = "MainScenePlayableRoot";
    private const string CoreAssetFolder = "Assets/ScriptableObjects/01_Core";
    private const string DefaultStagePath = CoreAssetFolder + "/DefaultStageData.asset";
    private const string DefaultSfxLibraryPath = "Assets/ScriptableObjects/04_UISoundCamera/DefaultSfxLibrary.asset";
    private const string KeyboardDataPath = CoreAssetFolder + "/KeyboardTargetData.asset";
    private const string PhoneDataPath = CoreAssetFolder + "/PhoneTargetData.asset";
    private const string WaterDataPath = CoreAssetFolder + "/WaterDispenserTargetData.asset";
    private const string PrinterDataPath = CoreAssetFolder + "/PrinterTargetData.asset";
    private const string LightSwitchDataPath = CoreAssetFolder + "/LightSwitchTargetData.asset";
    private const string MicrophoneDataPath = CoreAssetFolder + "/MicrophoneTargetData.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/02_Player/PlayerCat.prefab";
    private const string SupervisorPrefabPath = "Assets/Prefabs/03_NPCAI/Final NPCs/Supervisor.prefab";
    private const string MainCanvasPrefabPath = "Assets/Prefabs/05_UI/MainCanvas.prefab";

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
        AudioSfxLibrary sfxLibrary = CreateOrLoadDefaultSfxLibrary();
        MischiefTargetData keyboardData = CreateOrLoadTargetData(KeyboardDataPath, "Keyboard", MischiefType.Press, 10f, 8f, "Supervisor");
        MischiefTargetData phoneData = CreateOrLoadTargetData(PhoneDataPath, "Phone", MischiefType.Press, 15f, 8f, "Supervisor");
        MischiefTargetData waterData = CreateOrLoadTargetData(WaterDataPath, "WaterDispenser", MischiefType.Push, 15f, 8f, "Supervisor");
        MischiefTargetData printerData = CreateOrLoadTargetData(PrinterDataPath, "Printer", MischiefType.Press, 15f, 8f, "");
        MischiefTargetData lightSwitchData = CreateOrLoadTargetData(LightSwitchDataPath, "LightSwitch", MischiefType.Press, 5f, 8f, "");
        MischiefTargetData microphoneData = CreateOrLoadTargetData(MicrophoneDataPath, "Microphone", MischiefType.Meow, 12f, 10f, "");

        GameObject root = CreateEmpty(RootName, null, Vector3.zero);
        CreateEnvironment(root.transform);
        GameObject systems = CreateSystems(root.transform, stageData, sfxLibrary);
        GameObject player = CreatePlayer(root.transform);
        Light officeLight = CreateOfficeLight(root.transform);
        CreateMischiefTarget("Keyboard", root.transform, keyboardData, new Vector3(0f, 0.92f, 2f), new Vector3(0.7f, 0.08f, 0.25f));
        CreateMischiefTarget("Phone", root.transform, phoneData, new Vector3(1.1f, 0.92f, 2f), new Vector3(0.25f, 0.12f, 0.25f));
        CreateMischiefTarget("WaterDispenser", root.transform, waterData, new Vector3(-2.8f, 0.7f, 1.6f), new Vector3(0.45f, 1.4f, 0.45f), MischiefWorldEventType.WaterDispenserMess, MischiefWorldEventResolveMode.NearestCleaner);
        CreateMischiefTarget("Printer", root.transform, printerData, new Vector3(2.7f, 0.45f, 1.4f), new Vector3(0.8f, 0.5f, 0.6f), MischiefWorldEventType.PrinterMess, MischiefWorldEventResolveMode.NearestCleaner);
        CreateMischiefTarget("LightSwitch", root.transform, lightSwitchData, new Vector3(-3.75f, 1.2f, 0.6f), new Vector3(0.12f, 0.35f, 0.25f), MischiefWorldEventType.LightToggle, MischiefWorldEventResolveMode.NearestNpc, officeLight);
        CreateMischiefTarget("Microphone", root.transform, microphoneData, new Vector3(2.3f, 0.95f, -1.8f), new Vector3(0.18f, 0.45f, 0.18f), MischiefWorldEventType.MicrophoneBroadcast, MischiefWorldEventResolveMode.AllNpcs, null, true, 4f, "microphone_broadcast_meow", "");
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


    private static Light CreateOfficeLight(Transform parent)
    {
        GameObject lightObject = CreateEmpty("OfficePointLight", parent, new Vector3(0f, 2.6f, 0.4f));
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = 1.4f;
        light.color = new Color(1f, 0.95f, 0.75f);
        light.enabled = false;
        lightObject.AddComponent<LightSwitchControlledLight>();
        return light;
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

    private static GameObject CreateSystems(Transform parent, StageData stageData, AudioSfxLibrary sfxLibrary)
    {
        GameObject systems = CreateEmpty("Systems", parent, Vector3.zero);

        UIManager uiManager = systems.AddComponent<UIManager>();
        uiManager.UseMinimalOverlayMode();
        GameInputGate inputGate = systems.AddComponent<GameInputGate>();
        DesignUiRefs designUi = CreateDesignedUi(systems.transform);
        DesignedUICoreConnector designedUiConnector = designUi.connector != null ? designUi.connector : systems.AddComponent<DesignedUICoreConnector>();
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
        gameManager.uiBridgeBehaviour = designedUiConnector;

        stageManager.gameManager = gameManager;
        stageManager.scoreManager = scoreManager;
        stageManager.objectiveManager = objectiveManager;
        stageManager.failManager = failManager;
        stageManager.uiBridgeBehaviour = designedUiConnector;
        stageManager.defaultStageData = stageData;

        scoreManager.uiBridgeBehaviour = designedUiConnector;
        scoreManager.autoTick = true;

        rageManager.scoreManager = scoreManager;
        rageManager.uiBridgeBehaviour = designedUiConnector;

        objectiveManager.scoreManager = scoreManager;
        objectiveManager.stageManager = stageManager;
        objectiveManager.uiBridgeBehaviour = designedUiConnector;

        failManager.stageManager = stageManager;
        failManager.objectiveManager = objectiveManager;
        failManager.caughtRule = stageData.caughtRule;

        hidingManager.scoreManager = scoreManager;
        hidingManager.uiBridgeBehaviour = designedUiConnector;
        hidingManager.autoTick = true;
        hidingManager.ConfigureFromStageData(stageData);

        mischiefManager.rageManager = rageManager;
        mischiefManager.scoreManager = scoreManager;
        mischiefManager.objectiveManager = objectiveManager;
        mischiefManager.uiBridgeBehaviour = designedUiConnector;
        mischiefManager.autoTickTargetCooldowns = true;

        coreFacade.gameManager = gameManager;
        coreFacade.stageManager = stageManager;
        coreFacade.mischiefManager = mischiefManager;
        coreFacade.scoreManager = scoreManager;
        coreFacade.rageManager = rageManager;
        coreFacade.objectiveManager = objectiveManager;
        coreFacade.failManager = failManager;
        coreFacade.hidingManager = hidingManager;
        coreFacade.uiBridgeBehaviour = designedUiConnector;
        coreFacade.cuteActionRadius = 5f;
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
        validator.uiBridgeBehaviour = designedUiConnector;
        validator.logValidationOnStart = true;

        SetPrivateField(audioManager, "sfxSource", sfxSource);
        SetPrivateField(audioManager, "bgmSource", bgmSource);
        audioManager.SetSfxLibrary(sfxLibrary);
        SetPrivateField(uiManager, "coreFacade", coreFacade);
        SetPrivateField(uiManager, "feedbackAudio", feedbackAudio);
        SetPrivateField(designedUiConnector, "coreFacade", coreFacade);
        SetPrivateField(designedUiConnector, "hudManager", designUi.hudManager);
        SetPrivateField(designedUiConnector, "skillCooldownUI", designUi.skillCooldownUI);
        SetPrivateField(designedUiConnector, "completeUI", designUi.completeUI);
        SetPrivateField(designedUiConnector, "pauseMenu", designUi.pauseMenu);
        SetPrivateField(designedUiConnector, "fallbackFeedbackUI", uiManager);
        if (designUi.hudManager != null) designUi.hudManager.BindCore(coreFacade);
        if (designUi.completeUI != null) designUi.completeUI.BindCore(coreFacade);
        SetPrivateField(starter, "coreFacade", coreFacade);
        SetPrivateField(starter, "uiManager", uiManager);
        SetPrivateField(starter, "stageData", stageData);
        _ = inputGate;
        _ = designedUiConnector;

        return systems;
    }


    private sealed class DesignUiRefs
    {
        public HUDManager hudManager;
        public SkillCooldownUI skillCooldownUI;
        public CompleteUI completeUI;
        public PauseMenu pauseMenu;
        public DesignedUICoreConnector connector;
    }

    private static DesignUiRefs CreateDesignedUi(Transform parent)
    {
        EnsureEventSystem();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainCanvasPrefabPath);
        if (prefab != null)
        {
            RemoveOldGeneratedUiRoots();

            GameObject canvasObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create MainCanvas UI");
            canvasObject.name = "MainCanvas";
            canvasObject.transform.SetParent(parent, false);
            NormalizeCanvasSorting(canvasObject);
            MainCanvasUiRepair.Repair(canvasObject);
            return CollectDesignedUiRefs(canvasObject);
        }

        Debug.LogWarning("MainCanvas.prefab was not found. Falling back to generated minimal designed UI.");
        return CreateGeneratedDesignedUi(parent);
    }

    private static DesignUiRefs CollectDesignedUiRefs(GameObject root)
    {
        DesignUiRefs refs = new DesignUiRefs();
        refs.hudManager = root.GetComponentInChildren<HUDManager>(true);
        refs.skillCooldownUI = root.GetComponentInChildren<SkillCooldownUI>(true);
        refs.completeUI = root.GetComponentInChildren<CompleteUI>(true);
        refs.pauseMenu = root.GetComponentInChildren<PauseMenu>(true);
        refs.connector = root.GetComponentInChildren<DesignedUICoreConnector>(true);

        if (refs.connector == null)
        {
            refs.connector = root.AddComponent<DesignedUICoreConnector>();
        }

        return refs;
    }

    private static void NormalizeCanvasSorting(GameObject root)
    {
        if (root != null)
        {
            root.SetActive(true);
            root.transform.localScale = Vector3.one;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.localScale = Vector3.one;
            }
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].gameObject.SetActive(true);
            canvases[i].enabled = true;
            canvases[i].sortingOrder = Mathf.Max(canvases[i].sortingOrder, 4500 + i);
        }
    }

    private static void RemoveOldGeneratedUiRoots()
    {
        string[] names = { "PauseMenuCanvas", "DesignedUICanvas", "Generated PauseMenu", "Legacy PauseMenu" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject obj = GameObject.Find(names[i]);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private static DesignUiRefs CreateGeneratedDesignedUi(Transform parent)
    {
        EnsureEventSystem();

        GameObject canvasObject = CreateEmpty("DesignedUICanvas", parent, Vector3.zero);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        DesignUiRefs refs = new DesignUiRefs();
        refs.connector = canvasObject.AddComponent<DesignedUICoreConnector>();
        refs.hudManager = canvasObject.AddComponent<HUDManager>();
        refs.completeUI = canvasObject.AddComponent<CompleteUI>();
        refs.pauseMenu = canvasObject.AddComponent<PauseMenu>();

        TMP_Text scoreText = CreateTMPLabel(canvasObject.transform, "ScoreText", new Vector2(40f, -40f), new Vector2(260f, 46f), "0", 32, TextAlignmentOptions.Left);
        TMP_Text multiplierText = CreateTMPLabel(canvasObject.transform, "MultiplierText", new Vector2(40f, -86f), new Vector2(260f, 36f), "1.0x", 24, TextAlignmentOptions.Left);
        TMP_Text targetText = CreateTMPLabel(canvasObject.transform, "TargetScoreText", new Vector2(40f, -126f), new Vector2(360f, 36f), "目标：5000", 24, TextAlignmentOptions.Left);
        SetTopLeft(scoreText.rectTransform);
        SetTopLeft(multiplierText.rectTransform);
        SetTopLeft(targetText.rectTransform);

        refs.hudManager.scoreText = scoreText;
        refs.hudManager.multiplierText = multiplierText;
        refs.hudManager.targetScoreText = targetText;

        GameObject skillRoot = CreateUiPanel(canvasObject.transform, "SkillCooldownPanel", new Vector2(-96f, 92f), new Vector2(92f, 92f), new Color(0f, 0f, 0f, 0.35f));
        RectTransform skillRect = skillRoot.GetComponent<RectTransform>();
        skillRect.anchorMin = new Vector2(1f, 0f);
        skillRect.anchorMax = new Vector2(1f, 0f);
        refs.skillCooldownUI = skillRoot.AddComponent<SkillCooldownUI>();
        Image mask = skillRoot.GetComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.55f);
        mask.type = Image.Type.Filled;
        mask.fillMethod = Image.FillMethod.Radial360;
        mask.fillOrigin = 2;
        mask.fillAmount = 0f;
        TMP_Text cooldownText = CreateTMPLabel(skillRoot.transform, "CuteCooldownText", Vector2.zero, new Vector2(90f, 90f), "", 28, TextAlignmentOptions.Center);
        CenterStretch(cooldownText.rectTransform);
        refs.skillCooldownUI.cooldownMask = mask;
        refs.skillCooldownUI.cooldownText = cooldownText;

        GameObject pausePanel = CreateUiPanel(canvasObject.transform, "PausePanel", Vector2.zero, new Vector2(520f, 420f), new Color(0f, 0f, 0f, 0.78f));
        Center(pausePanel.GetComponent<RectTransform>());
        CreateTMPLabel(pausePanel.transform, "PauseTitle", new Vector2(0f, 130f), new Vector2(420f, 60f), "PAUSED", 42, TextAlignmentOptions.Center);
        CreateUiButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, 45f), refs.pauseMenu.ResumeGame);
        CreateUiButton(pausePanel.transform, "RestartButton", "Restart", new Vector2(0f, -25f), refs.pauseMenu.RestartScene);
        CreateUiButton(pausePanel.transform, "MenuButton", "Title", new Vector2(0f, -95f), refs.pauseMenu.GoToMainMenu);
        refs.pauseMenu.pausePanel = pausePanel;
        pausePanel.SetActive(false);

        GameObject completePanel = CreateUiPanel(canvasObject.transform, "CompletePanel", Vector2.zero, new Vector2(620f, 360f), new Color(0f, 0f, 0f, 0.82f));
        Center(completePanel.GetComponent<RectTransform>());
        TMP_Text resultTitle = CreateTMPLabel(completePanel.transform, "ResultTitle", new Vector2(0f, 105f), new Vector2(520f, 60f), "CLEAR", 44, TextAlignmentOptions.Center);
        TMP_Text resultScore = CreateTMPLabel(completePanel.transform, "ResultScore", new Vector2(0f, 40f), new Vector2(520f, 44f), "0", 34, TextAlignmentOptions.Center);
        TMP_Text resultDetail = CreateTMPLabel(completePanel.transform, "ResultDetail", new Vector2(0f, -18f), new Vector2(520f, 42f), "Stage Complete", 24, TextAlignmentOptions.Center);
        CreateUiButton(completePanel.transform, "RetryButton", "Retry", new Vector2(-145f, -105f), refs.completeUI.RetryCurrentScene);
        CreateUiButton(completePanel.transform, "LevelSelectButton", "Levels", new Vector2(145f, -105f), refs.completeUI.BackToLevelSelect);
        refs.completeUI.completePanel = completePanel;
        refs.completeUI.titleText = resultTitle;
        refs.completeUI.scoreText = resultScore;
        refs.completeUI.detailText = resultDetail;
        completePanel.SetActive(false);

        return refs;
    }

    private static GameObject CreateUiPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(panel, "Create UI Panel");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TMP_Text CreateTMPLabel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(labelObject, "Create TMP Label");
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static Button CreateUiButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiPanel(parent, name, anchoredPosition, new Vector2(220f, 52f), new Color(1f, 1f, 1f, 0.9f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        TMP_Text buttonLabel = CreateTMPLabel(buttonObject.transform, name + "Text", Vector2.zero, new Vector2(220f, 52f), label, 24, TextAlignmentOptions.Center);
        buttonLabel.color = Color.black;
        CenterStretch(buttonLabel.rectTransform);
        return button;
    }

    private static void SetTopLeft(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
    }

    private static void Center(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void CenterStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
        ThirdPersonCameraController cameraController = Object.FindObjectOfType<ThirdPersonCameraController>();
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
        cute.radius = 5f;
        cute.rageReduction = 20f;
        cute.cooldown = 20f;

        PlayerHide hide = player.GetComponent<PlayerHide>();
        SetPrivateField(hide, "playerController", controller);
        SetPrivateField(hide, "playerInteraction", interaction);
        SetPrivateField(hide, "coreFacade", coreFacade);
        SetPrivateField(hide, "hidingManager", hidingManager);
        SetPrivateField(hide, "uiManager", uiManager);
        SetPrivateField(hide, "cameraController", cameraController);
        SetPrivateField(hide, "animationController", animationController);
        SetPrivateField(hide, "sfxController", sfxController);

        SetPrivateField(sfxController, "audioManager", audioManager);
    }

    private static void CreateMischiefTarget(
        string name,
        Transform parent,
        MischiefTargetData data,
        Vector3 position,
        Vector3 scale,
        MischiefWorldEventType eventType = MischiefWorldEventType.None,
        MischiefWorldEventResolveMode resolveMode = MischiefWorldEventResolveMode.None,
        Light controlledLight = null,
        bool autoCompleteWorldEvent = false,
        float autoCompleteCooldown = 0f,
        string startSfxId = "",
        string completeSfxId = "")
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

        if (eventType != MischiefWorldEventType.None)
        {
            MischiefWorldEventReporter reporter = targetObject.AddComponent<MischiefWorldEventReporter>();
            bool disableAfterNpcResponse = eventType == MischiefWorldEventType.PrinterMess
                || eventType == MischiefWorldEventType.WaterDispenserMess
                || eventType == MischiefWorldEventType.GenericMess;
            reporter.Configure(eventType, resolveMode, disableAfterNpcResponse, autoCompleteWorldEvent, autoCompleteCooldown, startSfxId, completeSfxId);

            if (controlledLight != null)
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                reporter.ConfigureLights(new[] { controlledLight }, renderer != null ? new[] { renderer } : null);
            }
        }
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
        GameObject hideCameraAnchor = CreateEmpty("HideCameraAnchor", box.transform, new Vector3(0f, 0.45f, -0.48f));
        hideCameraAnchor.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        SetPrivateField(hideSpot, "interactionId", "HideSpot_Box");
        SetPrivateField(hideSpot, "hidePoint", hidePoint.transform);
        SetPrivateField(hideSpot, "hideCameraAnchor", hideCameraAnchor.transform);
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
        stageData.targetScore = 5000;
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

    private static AudioSfxLibrary CreateOrLoadDefaultSfxLibrary()
    {
        EnsureAssetFolder("Assets/ScriptableObjects");
        EnsureAssetFolder("Assets/ScriptableObjects/04_UISoundCamera");

        AudioSfxLibrary library = AssetDatabase.LoadAssetAtPath<AudioSfxLibrary>(DefaultSfxLibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<AudioSfxLibrary>();
            AssetDatabase.CreateAsset(library, DefaultSfxLibraryPath);
        }

        string[] ids = new[]
        {
            "ui_select",
            "ui_error",
            "ui_clear",
            "ui_fail",
            "cat_mischief",
            "cat_cute",
            "cat_jump",
            "cat_hide_enter",
            "cat_hide_exit",
            "cat_caught",
            "cat_meow",
            "microphone_broadcast_meow",
            "world_light_on",
            "world_mess_start",
            "world_mess_clean"
        };

        SerializedObject serialized = new SerializedObject(library);
        SerializedProperty entries = serialized.FindProperty("entries");
        if (entries != null && entries.arraySize == 0)
        {
            entries.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                SerializedProperty element = entries.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = ids[i];
                element.FindPropertyRelative("clip").objectReferenceValue = null;
                element.FindPropertyRelative("volume").floatValue = 1f;
                element.FindPropertyRelative("pitch").floatValue = 1f;
                element.FindPropertyRelative("spatial").boolValue = ids[i].StartsWith("world_") || ids[i].StartsWith("microphone_");
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
        }

        return library;
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
