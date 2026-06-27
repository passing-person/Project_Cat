using UnityEngine;

public class CoreSmokeTestRunner : MonoBehaviour
{
    [Header("Managers")]
    public GameManager gameManager;
    public StageManager stageManager;
    public MischiefManager mischiefManager;
    public ScoreManager scoreManager;
    public RageManager rageManager;
    public ObjectiveManager objectiveManager;
    public FailManager failManager;
    public HidingManager hidingManager;
    public CoreFacade coreFacade;
    public CoreReferenceValidator referenceValidator;

    [Header("Mocks")]
    public MockRageReceiver supervisor;
    public MockMischiefSource keyboardSource;
    public MockUIBridge uiBridge;
    public StageData stageData;

    [Header("Run")]
    public bool runOnStart = true;
    public bool runCaughtRuleTest = true;

    private int passCount;
    private int failCount;

    private void Start()
    {
        if (runOnStart)
        {
            RunAllTests();
        }
    }

    [ContextMenu("Run Core Smoke Test")]
    public void RunAllTests()
    {
        passCount = 0;
        failCount = 0;

        ResolveReferences();
        WireReferences();
        ResetMocks();

        Debug.Log("========== Core Smoke Test Started ==========");

        RunSetupTest();
        RunRageAndScoreTest();
        RunThresholdTest();
        RunTargetScoreTest();
        RunCuteRageReductionTest();
        RunTargetLockTest();
        RunTargetStateTest();
        RunHidingCoreTest();
        RunSecurityMultiplierOverrideTest();
        RunFacadeIntegrationTest();
        RunReferenceValidatorTest();

        if (runCaughtRuleTest)
        {
            RunCaughtRuleTest();
        }

        Debug.Log($"========== Core Smoke Test Finished: PASS {passCount}, FAIL {failCount} ==========");
    }

    private void ResolveReferences()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (mischiefManager == null) mischiefManager = FindObjectOfType<MischiefManager>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        if (rageManager == null) rageManager = FindObjectOfType<RageManager>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (failManager == null) failManager = FindObjectOfType<FailManager>();
        if (hidingManager == null) hidingManager = FindObjectOfType<HidingManager>();
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (referenceValidator == null) referenceValidator = FindObjectOfType<CoreReferenceValidator>();
        if (supervisor == null) supervisor = FindObjectOfType<MockRageReceiver>();
        if (keyboardSource == null) keyboardSource = FindObjectOfType<MockMischiefSource>();
        if (uiBridge == null) uiBridge = FindObjectOfType<MockUIBridge>();
    }

    private void WireReferences()
    {
        if (gameManager != null)
        {
            gameManager.stageManager = stageManager;
            gameManager.uiBridgeBehaviour = uiBridge;
        }

        if (stageManager != null)
        {
            stageManager.gameManager = gameManager;
            stageManager.scoreManager = scoreManager;
            stageManager.objectiveManager = objectiveManager;
            stageManager.failManager = failManager;
            stageManager.uiBridgeBehaviour = uiBridge;
            stageManager.defaultStageData = stageData;
        }

        if (mischiefManager != null)
        {
            mischiefManager.rageManager = rageManager;
            mischiefManager.scoreManager = scoreManager;
            mischiefManager.objectiveManager = objectiveManager;
            mischiefManager.uiBridgeBehaviour = uiBridge;
            mischiefManager.autoTickTargetCooldowns = false;
        }

        if (scoreManager != null)
        {
            scoreManager.uiBridgeBehaviour = uiBridge;
            scoreManager.autoTick = false;
        }

        if (rageManager != null)
        {
            rageManager.scoreManager = scoreManager;
            rageManager.uiBridgeBehaviour = uiBridge;
        }

        if (objectiveManager != null)
        {
            objectiveManager.scoreManager = scoreManager;
            objectiveManager.stageManager = stageManager;
            objectiveManager.uiBridgeBehaviour = uiBridge;
        }

        if (failManager != null)
        {
            failManager.stageManager = stageManager;
            failManager.objectiveManager = objectiveManager;
        }

        if (hidingManager != null)
        {
            hidingManager.scoreManager = scoreManager;
            hidingManager.uiBridgeBehaviour = uiBridge;
            hidingManager.autoTick = false;
        }

        if (coreFacade != null)
        {
            coreFacade.gameManager = gameManager;
            coreFacade.stageManager = stageManager;
            coreFacade.mischiefManager = mischiefManager;
            coreFacade.scoreManager = scoreManager;
            coreFacade.rageManager = rageManager;
            coreFacade.objectiveManager = objectiveManager;
            coreFacade.failManager = failManager;
            coreFacade.hidingManager = hidingManager;
            coreFacade.uiBridgeBehaviour = uiBridge;
            coreFacade.cuteActionRadius = 5f;
            coreFacade.cuteActionRageReduction = 20f;
            coreFacade.defaultSecurityMultiplier = stageData != null ? stageData.securityMultiplierOverride : 13f;
            coreFacade.autoResolveReferences = false;
            coreFacade.autoWireReferences = false;
            coreFacade.WireReferences();
        }

        if (referenceValidator != null)
        {
            referenceValidator.coreFacade = coreFacade;
            referenceValidator.gameManager = gameManager;
            referenceValidator.stageManager = stageManager;
            referenceValidator.mischiefManager = mischiefManager;
            referenceValidator.scoreManager = scoreManager;
            referenceValidator.rageManager = rageManager;
            referenceValidator.objectiveManager = objectiveManager;
            referenceValidator.failManager = failManager;
            referenceValidator.hidingManager = hidingManager;
            referenceValidator.uiBridgeBehaviour = uiBridge;
        }
    }

    private void ResetMocks()
    {
        if (uiBridge != null) uiBridge.ResetMockValues();
        if (supervisor != null) supervisor.ResetMockFlags();
    }

    private void PrepareFreshStage(ObjectiveType objectiveType, CaughtRule caughtRule, int targetScore = 100)
    {
        if (stageData != null)
        {
            stageData.objectiveType = objectiveType;
            stageData.caughtRule = caughtRule;
            stageData.targetScore = targetScore;
            stageData.baseScoreRate = 10f;
            stageData.maxScoreMultiplierBonus = 12f;
            stageData.securityMultiplierOverride = 13f;
            stageData.maxHideDuration = 0f;
            stageData.hiddenMultiplierScale = 0.1f;
            stageData.hideSpotUsesPerStage = 0;
            stageData.NormalizeValues();
        }

        if (stageManager != null)
        {
            stageManager.LoadStage(stageData);
            stageManager.StartStage();
        }

        if (rageManager != null)
        {
            rageManager.ResetAllRage();
        }

        if (mischiefManager != null)
        {
            mischiefManager.ResetTargetStates();
        }

        if (hidingManager != null)
        {
            hidingManager.ConfigureFromStageData(stageData);
            hidingManager.ResetHidingState();
        }

        if (supervisor != null)
        {
            supervisor.ResetMockFlags();
        }

        if (uiBridge != null)
        {
            uiBridge.stageClearShown = false;
            uiBridge.stageFailedShown = false;
        }
    }

    private void RunSetupTest()
    {
        AssertNotNull("ScoreManager exists", scoreManager);
        AssertNotNull("RageManager exists", rageManager);
        AssertNotNull("MischiefManager exists", mischiefManager);
        AssertNotNull("HidingManager exists", hidingManager);
        AssertNotNull("CoreFacade exists", coreFacade);
        AssertNotNull("CoreReferenceValidator exists", referenceValidator);
        AssertNotNull("Supervisor mock exists", supervisor);
        AssertNotNull("Keyboard source exists", keyboardSource);

        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);

        if (rageManager != null && supervisor != null)
        {
            rageManager.RegisterNpc(supervisor);
        }

        AssertApprox("Initial rage is zero", rageManager.GetRage(supervisor.NpcId), 0f, 0.001f);
        AssertTrue("Score is not ticking before rage", !scoreManager.IsScoring);

        scoreManager.TickScore(1f);
        AssertApprox("Score stays zero before rage", scoreManager.CurrentScoreFloat, 0f, 0.001f);
    }

    private void RunRageAndScoreTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);

        MischiefContext context = keyboardSource.CreateContext("TestCat");
        bool applied = mischiefManager.ApplyMischief(context);

        AssertTrue("Mischief is applied", applied);
        AssertApprox("Supervisor rage increased by target data", rageManager.GetRage(supervisor.NpcId), keyboardSource.BaseRageAmount, 0.001f);
        AssertTrue("Score starts after rage", scoreManager.IsScoring);

        float expectedAverage = rageManager.GetAverageRage();
        float expectedMultiplier = 1f + Mathf.Pow(expectedAverage / 100f, 2f) * stageData.maxScoreMultiplierBonus;
        AssertApprox("Score multiplier follows average rage formula", scoreManager.CurrentMultiplier, expectedMultiplier, 0.001f);

        float previousScore = scoreManager.CurrentScoreFloat;
        scoreManager.TickScore(1f);
        AssertTrue("Score increases after ticking", scoreManager.CurrentScoreFloat > previousScore);
    }

    private void RunThresholdTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);

        rageManager.SetRage(supervisor.NpcId, 40f);
        AssertTrue("40 rage changes state to Annoyed", rageManager.GetRageState(supervisor.NpcId) == NpcRageState.Annoyed);

        rageManager.SetRage(supervisor.NpcId, 70f);
        AssertTrue("70 rage changes state to Angry", rageManager.GetRageState(supervisor.NpcId) == NpcRageState.Angry);

        supervisor.startChaseCalled = false;
        rageManager.SetRage(supervisor.NpcId, 100f);
        AssertTrue("100 rage changes state to Enraged", rageManager.GetRageState(supervisor.NpcId) == NpcRageState.Enraged);
        AssertTrue("100 rage calls StartChase", supervisor.startChaseCalled);
    }

    private void RunTargetScoreTest()
    {
        PrepareFreshStage(ObjectiveType.ReachScore, CaughtRule.AlwaysFail, 100);

        AssertTrue("Target score is not reached at start", !scoreManager.HasReachedTargetScore());
        scoreManager.AddScore(100);
        AssertTrue("Target score is reached after AddScore", scoreManager.HasReachedTargetScore());
        AssertTrue("ObjectiveManager reports enough score", objectiveManager.HasEnoughScore());
    }

    private void RunCuteRageReductionTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);

        rageManager.SetRage(supervisor.NpcId, 50f);
        rageManager.ReduceRage(supervisor.NpcId, 20f);
        AssertApprox("ReduceRage lowers NPC rage", rageManager.GetRage(supervisor.NpcId), 30f, 0.001f);
        AssertTrue("Reduced rage updates state", rageManager.GetRageState(supervisor.NpcId) == NpcRageState.Calm);

        rageManager.SetRage(supervisor.NpcId, 50f);
        var results = rageManager.ReduceRageAround(supervisor.Position, 5f, 20f);
        AssertTrue("ReduceRageAround affects nearby NPC", results.Count > 0);
        AssertApprox("ReduceRageAround lowers nearby NPC rage", rageManager.GetRage(supervisor.NpcId), 30f, 0.001f);
    }

    private void RunTargetLockTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);

        string targetId = keyboardSource.InteractionId;
        mischiefManager.UnlockMischiefTarget(targetId);
        AssertTrue("Target is usable before lock", mischiefManager.CanApplyMischief(targetId));

        mischiefManager.LockMischiefTarget(targetId);
        AssertTrue("Target is locked after LockMischiefTarget", !mischiefManager.CanApplyMischief(targetId));

        bool applied = mischiefManager.ApplyMischief(keyboardSource.CreateContext("TestCat"));
        AssertTrue("Locked target blocks mischief", !applied);

        mischiefManager.UnlockMischiefTarget(targetId);
        AssertTrue("Target is usable after unlock", mischiefManager.CanApplyMischief(targetId));
    }

    private void RunTargetStateTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        string targetId = keyboardSource.InteractionId;

        AssertTrue("Target state defaults to Available", mischiefManager.GetMischiefTargetState(targetId) == MischiefTargetState.Available);

        mischiefManager.StartMischiefTargetCooldown(targetId, 2f);
        AssertTrue("Cooldown target blocks mischief", !mischiefManager.CanApplyMischief(targetId));
        AssertTrue("Cooldown target reports Cooldown state", mischiefManager.GetMischiefTargetState(targetId) == MischiefTargetState.Cooldown);

        mischiefManager.TickTargetCooldowns(1f);
        AssertTrue("Cooldown remains before duration ends", mischiefManager.GetMischiefTargetState(targetId) == MischiefTargetState.Cooldown);

        mischiefManager.TickTargetCooldowns(1.1f);
        AssertTrue("Cooldown target becomes available after duration", mischiefManager.GetMischiefTargetState(targetId) == MischiefTargetState.Available);

        mischiefManager.DisableMischiefTarget(targetId);
        AssertTrue("Disabled target blocks mischief", !mischiefManager.CanApplyMischief(targetId));
        AssertTrue("Disabled target reports Disabled state", mischiefManager.GetMischiefTargetState(targetId) == MischiefTargetState.Disabled);

        coreFacade.SetMischiefTargetState(targetId, MischiefTargetState.Available);
        AssertTrue("CoreFacade can restore target availability", coreFacade.CanApplyMischief(targetId));
    }

    private void RunHidingCoreTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);
        rageManager.SetRage(supervisor.NpcId, 50f);

        const string hideSpotId = "CardboardBox";
        AssertTrue("Hide spot is usable before first use", hidingManager.CanUseHideSpot(hideSpotId));

        bool entered = coreFacade.ReportPlayerHidden(hideSpotId);
        AssertTrue("CoreFacade.ReportPlayerHidden enters hiding", entered);
        AssertTrue("HidingManager tracks hidden state", hidingManager.IsHidden);
        AssertTrue("Cannot enter hide while already hidden", !hidingManager.CanUseHideSpot(hideSpotId));
        AssertApprox("Hidden multiplier scale is applied", scoreManager.TemporaryMultiplierScale, 0.1f, 0.001f);
        AssertApprox("Visible multiplier becomes 10 percent", scoreManager.CurrentMultiplier, 0.4f, 0.001f);

        coreFacade.ReportPlayerExitHiding();
        AssertTrue("Manual exit leaves hiding state", !hidingManager.IsHidden);
        AssertTrue("Hide spot is usable again after manual exit", hidingManager.CanUseHideSpot(hideSpotId));

        entered = coreFacade.ReportPlayerHidden(hideSpotId);
        AssertTrue("Player can hide again after manual exit", entered);

        float previousScore = scoreManager.CurrentScoreFloat;
        scoreManager.StartScoring();
        scoreManager.TickScore(1f);
        AssertApprox("Hidden score ticking uses scaled multiplier", scoreManager.CurrentScoreFloat - previousScore, 4f, 0.001f);

        hidingManager.TickHiding(30f);
        AssertTrue("Hiding does not auto-exit without duration limit", hidingManager.IsHidden);
        AssertApprox("Multiplier scale stays active while hidden", scoreManager.TemporaryMultiplierScale, 0.1f, 0.001f);

        coreFacade.ReportPlayerExitHiding();
        AssertApprox("Multiplier scale is cleared after manual exit", scoreManager.TemporaryMultiplierScale, 1f, 0.001f);
        AssertTrue("Hide spot remains usable with no use limit", hidingManager.CanUseHideSpot(hideSpotId));
    }

    private void RunSecurityMultiplierOverrideTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        rageManager.RegisterNpc(supervisor);

        rageManager.SetRage(supervisor.NpcId, 50f);
        AssertApprox("Base multiplier at 50 rage is 4", scoreManager.CurrentMultiplier, 4f, 0.001f);

        rageManager.SetSecurityMultiplierOverride(true, stageData.securityMultiplierOverride);
        AssertApprox("Security override sets multiplier to 13", scoreManager.CurrentMultiplier, 13f, 0.001f);

        scoreManager.RecalculateMultiplier(50f);
        AssertApprox("Security override persists during recalculation", scoreManager.CurrentMultiplier, 13f, 0.001f);

        rageManager.SetSecurityMultiplierOverride(false, stageData.securityMultiplierOverride);
        AssertApprox("Disabling security override restores rage formula", scoreManager.CurrentMultiplier, 4f, 0.001f);
    }

    private void RunFacadeIntegrationTest()
    {
        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);

        AssertTrue("CoreFacade validates required references", coreFacade != null && coreFacade.ValidateCoreReferences(out _));

        coreFacade.RegisterRageReceiver(supervisor);
        bool applied = coreFacade.ApplyMischief(keyboardSource.CreateContext("FacadeCat"));
        AssertTrue("CoreFacade.ApplyMischief applies mischief", applied);
        AssertApprox("CoreFacade.ApplyMischief increases rage", rageManager.GetRage(supervisor.NpcId), keyboardSource.BaseRageAmount, 0.001f);

        coreFacade.TryCuteAction(supervisor.Position);
        AssertApprox("CoreFacade.TryCuteAction reduces rage", rageManager.GetRage(supervisor.NpcId), 30f, 0.001f);

        string targetId = keyboardSource.InteractionId;
        coreFacade.LockMischiefTarget(targetId);
        AssertTrue("CoreFacade.LockMischiefTarget locks target", !coreFacade.CanApplyMischief(targetId));
        coreFacade.UnlockMischiefTarget(targetId);
        AssertTrue("CoreFacade.UnlockMischiefTarget unlocks target", coreFacade.CanApplyMischief(targetId));

        coreFacade.StartMischiefTargetCooldown(targetId, 1f);
        AssertTrue("CoreFacade.StartMischiefTargetCooldown blocks target", !coreFacade.CanApplyMischief(targetId));
        coreFacade.SetMischiefTargetState(targetId, MischiefTargetState.Available);
        AssertTrue("CoreFacade.SetMischiefTargetState restores target", coreFacade.CanApplyMischief(targetId));

        bool hid = coreFacade.ReportPlayerHidden("FacadeBox");
        AssertTrue("CoreFacade.ReportPlayerHidden routes to HidingManager", hid && coreFacade.IsPlayerHidden);
        coreFacade.ReportPlayerExitHiding();
        AssertTrue("CoreFacade.ReportPlayerExitHiding clears hidden state", !coreFacade.IsPlayerHidden);

        rageManager.SetRage(supervisor.NpcId, 50f);
        coreFacade.SetSecurityMultiplierOverride(true);
        AssertApprox("CoreFacade.SetSecurityMultiplierOverride enables override", scoreManager.CurrentMultiplier, 13f, 0.001f);
        coreFacade.SetSecurityMultiplierOverride(false);
        AssertApprox("CoreFacade.SetSecurityMultiplierOverride disables override", scoreManager.CurrentMultiplier, 4f, 0.001f);

        coreFacade.ReportPlayerCaught();
        AssertTrue("CoreFacade.ReportPlayerCaught routes to FailManager", stageManager.StageFailed);
    }

    private void RunReferenceValidatorTest()
    {
        if (referenceValidator == null)
        {
            AssertTrue("Reference validator test skipped because validator is missing", false);
            return;
        }

        referenceValidator.ResolveReferences();
        bool valid = referenceValidator.ValidateCoreReferences(out string report);
        AssertTrue("CoreReferenceValidator reports valid core references", valid);
        AssertTrue("CoreReferenceValidator report is not empty", !string.IsNullOrWhiteSpace(report));
    }

    private void RunCaughtRuleTest()
    {
        if (stageData == null || failManager == null)
        {
            AssertTrue("Caught rule test skipped because dependencies are missing", true);
            return;
        }

        PrepareFreshStage(ObjectiveType.SurviveChase, CaughtRule.AlwaysFail, 100);
        failManager.HandlePlayerCaught();
        AssertTrue("AlwaysFail caught rule fails the stage", failManager.HasFailed && stageManager.StageFailed);

        PrepareFreshStage(ObjectiveType.ReachScore, CaughtRule.ClearIfEnoughScore, 100);
        failManager.HandlePlayerCaught();
        AssertTrue("ClearIfEnoughScore fails when score is not enough", failManager.HasFailed && stageManager.StageFailed);

        PrepareFreshStage(ObjectiveType.ReachScore, CaughtRule.ClearIfEnoughScore, 100);
        scoreManager.AddScore(100);
        failManager.HandlePlayerCaught();
        AssertTrue("ClearIfEnoughScore clears when score is enough", stageManager.StageCleared && !failManager.HasFailed);
    }

    private void AssertNotNull(string label, Object value)
    {
        AssertTrue(label, value != null);
    }

    private void AssertTrue(string label, bool condition)
    {
        if (condition)
        {
            passCount++;
            Debug.Log($"[PASS] {label}");
        }
        else
        {
            failCount++;
            Debug.LogError($"[FAIL] {label}");
        }
    }

    private void AssertApprox(string label, float actual, float expected, float tolerance)
    {
        bool condition = Mathf.Abs(actual - expected) <= tolerance;
        if (condition)
        {
            passCount++;
            Debug.Log($"[PASS] {label}: actual={actual:0.###}, expected={expected:0.###}");
        }
        else
        {
            failCount++;
            Debug.LogError($"[FAIL] {label}: actual={actual:0.###}, expected={expected:0.###}");
        }
    }
}
