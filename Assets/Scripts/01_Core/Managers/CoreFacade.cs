using System.Collections.Generic;
using UnityEngine;

public class CoreFacade : MonoBehaviour
{
    [Header("Core Managers")]
    public GameManager gameManager;
    public StageManager stageManager;
    public MischiefManager mischiefManager;
    public ScoreManager scoreManager;
    public RageManager rageManager;
    public ObjectiveManager objectiveManager;
    public FailManager failManager;

    [Header("External Bridge")]
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Cute Action")]
    public float cuteActionRadius = 5f;
    public float cuteActionRageReduction = 20f;

    [Header("Security")]
    public float defaultSecurityMultiplier = 13f;

    [Header("Options")]
    public bool autoResolveReferences = true;
    public bool autoWireReferences = true;

    public bool HasValidCoreReferences => ValidateCoreReferences(out _);
    public GameState CurrentGameState => gameManager != null ? gameManager.CurrentState : GameState.Boot;
    public StageData CurrentStageData => stageManager != null ? stageManager.CurrentStageData : null;
    public string CurrentStageId => stageManager != null ? stageManager.CurrentStageId : string.Empty;
    public int CurrentScore => scoreManager != null ? scoreManager.CurrentScore : 0;
    public float CurrentScoreFloat => scoreManager != null ? scoreManager.CurrentScoreFloat : 0f;
    public float CurrentMultiplier => scoreManager != null ? scoreManager.CurrentMultiplier : 1f;
    public bool IsStageFinished => stageManager != null && stageManager.IsStageFinished;
    public bool StageCleared => stageManager != null && stageManager.StageCleared;
    public bool StageFailed => stageManager != null && stageManager.StageFailed;

    private void Awake()
    {
        if (autoResolveReferences)
        {
            ResolveReferences();
        }

        if (autoWireReferences)
        {
            WireReferences();
        }
    }

    [ContextMenu("Resolve Core References")]
    public void ResolveReferences()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (mischiefManager == null) mischiefManager = FindObjectOfType<MischiefManager>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        if (rageManager == null) rageManager = FindObjectOfType<RageManager>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (failManager == null) failManager = FindObjectOfType<FailManager>();
    }

    [ContextMenu("Wire Core References")]
    public void WireReferences()
    {
        if (gameManager != null)
        {
            gameManager.stageManager = stageManager;
            if (uiBridgeBehaviour != null) gameManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (stageManager != null)
        {
            stageManager.gameManager = gameManager;
            stageManager.scoreManager = scoreManager;
            stageManager.objectiveManager = objectiveManager;
            stageManager.failManager = failManager;
            if (uiBridgeBehaviour != null) stageManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (mischiefManager != null)
        {
            mischiefManager.rageManager = rageManager;
            mischiefManager.scoreManager = scoreManager;
            mischiefManager.objectiveManager = objectiveManager;
            if (uiBridgeBehaviour != null) mischiefManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (scoreManager != null && uiBridgeBehaviour != null)
        {
            scoreManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (rageManager != null)
        {
            rageManager.scoreManager = scoreManager;
            if (uiBridgeBehaviour != null) rageManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (objectiveManager != null)
        {
            objectiveManager.scoreManager = scoreManager;
            objectiveManager.stageManager = stageManager;
            if (uiBridgeBehaviour != null) objectiveManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (failManager != null)
        {
            failManager.stageManager = stageManager;
            failManager.objectiveManager = objectiveManager;
        }

        ICoreUIBridge bridge = uiBridgeBehaviour as ICoreUIBridge;
        if (bridge != null)
        {
            SetUIBridge(uiBridgeBehaviour);
        }
    }

    public void SetUIBridge(MonoBehaviour bridgeBehaviour)
    {
        uiBridgeBehaviour = bridgeBehaviour;

        if (gameManager != null) gameManager.uiBridgeBehaviour = bridgeBehaviour;
        if (stageManager != null) stageManager.uiBridgeBehaviour = bridgeBehaviour;
        if (mischiefManager != null) mischiefManager.uiBridgeBehaviour = bridgeBehaviour;
        if (objectiveManager != null) objectiveManager.uiBridgeBehaviour = bridgeBehaviour;

        if (scoreManager != null)
        {
            scoreManager.uiBridgeBehaviour = bridgeBehaviour;
            scoreManager.SetUIBridge(bridgeBehaviour as ICoreUIBridge);
        }

        if (rageManager != null)
        {
            rageManager.uiBridgeBehaviour = bridgeBehaviour;
            rageManager.SetUIBridge(bridgeBehaviour as ICoreUIBridge);
        }
    }

    public void LoadStage(StageData stageData)
    {
        if (stageManager == null)
        {
            Debug.LogWarning("CoreFacade.LoadStage failed: StageManager is missing.");
            return;
        }

        stageManager.LoadStage(stageData);
    }

    public void StartStage()
    {
        if (stageManager == null)
        {
            Debug.LogWarning("CoreFacade.StartStage failed: StageManager is missing.");
            return;
        }

        stageManager.StartStage();
    }

    public bool ApplyMischief(MischiefContext context)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.ApplyMischief failed: MischiefManager is missing.");
            return false;
        }

        return mischiefManager.ApplyMischief(context);
    }

    public bool CanApplyMischief(string targetId)
    {
        return mischiefManager != null && mischiefManager.CanApplyMischief(targetId);
    }

    public void LockMischiefTarget(string targetId)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.LockMischiefTarget failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.LockMischiefTarget(targetId);
    }

    public void UnlockMischiefTarget(string targetId)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.UnlockMischiefTarget failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.UnlockMischiefTarget(targetId);
    }

    public void RegisterRageReceiver(IRageReceiver receiver)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.RegisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.RegisterNpc(receiver);
    }

    public void RegisterRageReceiver(string npcId, object receiverObject)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.RegisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.RegisterNpc(npcId, receiverObject);
    }

    public void UnregisterRageReceiver(IRageReceiver receiver)
    {
        if (receiver == null)
        {
            return;
        }

        UnregisterRageReceiver(receiver.NpcId);
    }

    public void UnregisterRageReceiver(string npcId)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.UnregisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.UnregisterNpc(npcId);
    }

    public List<RageResult> TryCuteAction(Vector3 origin)
    {
        return TryCuteAction(origin, cuteActionRadius, cuteActionRageReduction);
    }

    public List<RageResult> TryCuteAction(Vector3 origin, float radius, float rageReduction)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.TryCuteAction failed: RageManager is missing.");
            return new List<RageResult>();
        }

        return rageManager.ReduceRageAround(origin, radius, rageReduction);
    }

    public void ReportPlayerCaught()
    {
        if (failManager == null)
        {
            Debug.LogWarning("CoreFacade.ReportPlayerCaught failed: FailManager is missing.");
            return;
        }

        failManager.HandlePlayerCaught();
    }

    public int AddScore(int amount)
    {
        if (scoreManager == null)
        {
            Debug.LogWarning("CoreFacade.AddScore failed: ScoreManager is missing.");
            return 0;
        }

        return scoreManager.AddScore(amount);
    }

    public void SetSecurityMultiplierOverride(bool enabled)
    {
        SetSecurityMultiplierOverride(enabled, defaultSecurityMultiplier);
    }

    public void SetSecurityMultiplierOverride(bool enabled, float multiplier)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.SetSecurityMultiplierOverride failed: RageManager is missing.");
            return;
        }

        rageManager.SetSecurityMultiplierOverride(enabled, multiplier);
    }

    public bool ValidateCoreReferences(out string report)
    {
        List<string> missing = new List<string>();

        if (gameManager == null) missing.Add("GameManager");
        if (stageManager == null) missing.Add("StageManager");
        if (mischiefManager == null) missing.Add("MischiefManager");
        if (scoreManager == null) missing.Add("ScoreManager");
        if (rageManager == null) missing.Add("RageManager");
        if (objectiveManager == null) missing.Add("ObjectiveManager");
        if (failManager == null) missing.Add("FailManager");

        if (missing.Count > 0)
        {
            report = "CoreFacade is missing references: " + string.Join(", ", missing.ToArray());
            return false;
        }

        report = "CoreFacade references are valid.";
        return true;
    }
}
