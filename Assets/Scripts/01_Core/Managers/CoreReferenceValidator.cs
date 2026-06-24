using System.Collections.Generic;
using UnityEngine;

public class CoreReferenceValidator : MonoBehaviour
{
    [Header("Facade")]
    public CoreFacade coreFacade;

    [Header("Required Managers")]
    public GameManager gameManager;
    public StageManager stageManager;
    public MischiefManager mischiefManager;
    public ScoreManager scoreManager;
    public RageManager rageManager;
    public ObjectiveManager objectiveManager;
    public FailManager failManager;
    public HidingManager hidingManager;

    [Header("Optional External Bridge")]
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Options")]
    public bool autoResolveOnAwake = true;
    public bool logValidationOnStart = false;

    private void Awake()
    {
        if (autoResolveOnAwake)
        {
            ResolveReferences();
        }
    }

    private void Start()
    {
        if (logValidationOnStart)
        {
            LogValidation();
        }
    }

    [ContextMenu("Resolve References")]
    public void ResolveReferences()
    {
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();

        if (coreFacade != null)
        {
            coreFacade.ResolveReferences();
            gameManager = gameManager != null ? gameManager : coreFacade.gameManager;
            stageManager = stageManager != null ? stageManager : coreFacade.stageManager;
            mischiefManager = mischiefManager != null ? mischiefManager : coreFacade.mischiefManager;
            scoreManager = scoreManager != null ? scoreManager : coreFacade.scoreManager;
            rageManager = rageManager != null ? rageManager : coreFacade.rageManager;
            objectiveManager = objectiveManager != null ? objectiveManager : coreFacade.objectiveManager;
            failManager = failManager != null ? failManager : coreFacade.failManager;
            hidingManager = hidingManager != null ? hidingManager : coreFacade.hidingManager;
            uiBridgeBehaviour = uiBridgeBehaviour != null ? uiBridgeBehaviour : coreFacade.uiBridgeBehaviour;
        }

        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (mischiefManager == null) mischiefManager = FindObjectOfType<MischiefManager>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        if (rageManager == null) rageManager = FindObjectOfType<RageManager>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (failManager == null) failManager = FindObjectOfType<FailManager>();
        if (hidingManager == null) hidingManager = FindObjectOfType<HidingManager>();
    }

    public bool ValidateCoreReferences(out string report)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        if (coreFacade == null) warnings.Add("CoreFacade is missing. External teams may need to reference multiple managers directly.");
        if (gameManager == null) errors.Add("GameManager is missing.");
        if (stageManager == null) errors.Add("StageManager is missing.");
        if (mischiefManager == null) errors.Add("MischiefManager is missing.");
        if (scoreManager == null) errors.Add("ScoreManager is missing.");
        if (rageManager == null) errors.Add("RageManager is missing.");
        if (objectiveManager == null) errors.Add("ObjectiveManager is missing.");
        if (failManager == null) errors.Add("FailManager is missing.");
        if (hidingManager == null) warnings.Add("HidingManager is missing. Hiding core support will be disabled.");

        if (stageManager != null)
        {
            if (stageManager.gameManager == null) warnings.Add("StageManager.gameManager is not assigned.");
            if (stageManager.scoreManager == null) warnings.Add("StageManager.scoreManager is not assigned.");
            if (stageManager.objectiveManager == null) warnings.Add("StageManager.objectiveManager is not assigned.");
            if (stageManager.failManager == null) warnings.Add("StageManager.failManager is not assigned.");
        }

        if (mischiefManager != null)
        {
            if (mischiefManager.rageManager == null) warnings.Add("MischiefManager.rageManager is not assigned.");
            if (mischiefManager.scoreManager == null) warnings.Add("MischiefManager.scoreManager is not assigned.");
            if (mischiefManager.objectiveManager == null) warnings.Add("MischiefManager.objectiveManager is not assigned.");
        }

        if (rageManager != null && rageManager.scoreManager == null)
        {
            warnings.Add("RageManager.scoreManager is not assigned.");
        }

        if (objectiveManager != null)
        {
            if (objectiveManager.scoreManager == null) warnings.Add("ObjectiveManager.scoreManager is not assigned.");
            if (objectiveManager.stageManager == null) warnings.Add("ObjectiveManager.stageManager is not assigned.");
        }

        if (failManager != null)
        {
            if (failManager.stageManager == null) warnings.Add("FailManager.stageManager is not assigned.");
            if (failManager.objectiveManager == null) warnings.Add("FailManager.objectiveManager is not assigned.");
        }

        if (hidingManager != null && hidingManager.scoreManager == null)
        {
            warnings.Add("HidingManager.scoreManager is not assigned.");
        }

        if (uiBridgeBehaviour == null)
        {
            warnings.Add("UI bridge is not assigned. This is allowed for logic-only tests but real UI will not update.");
        }
        else if (!(uiBridgeBehaviour is ICoreUIBridge))
        {
            warnings.Add("UI bridge object does not implement ICoreUIBridge.");
        }

        bool valid = errors.Count == 0;
        report = BuildReport(valid, errors, warnings);
        return valid;
    }

    [ContextMenu("Log Validation")]
    public void LogValidation()
    {
        bool valid = ValidateCoreReferences(out string report);
        if (valid)
        {
            Debug.Log(report);
        }
        else
        {
            Debug.LogError(report);
        }
    }

    private string BuildReport(bool valid, List<string> errors, List<string> warnings)
    {
        string report = valid ? "Core reference validation passed." : "Core reference validation failed.";

        if (errors.Count > 0)
        {
            report += "\nErrors:";
            for (int i = 0; i < errors.Count; i++)
            {
                report += "\n- " + errors[i];
            }
        }

        if (warnings.Count > 0)
        {
            report += "\nWarnings:";
            for (int i = 0; i < warnings.Count; i++)
            {
                report += "\n- " + warnings[i];
            }
        }

        return report;
    }
}
