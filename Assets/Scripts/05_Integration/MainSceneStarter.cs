using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class MainSceneStarter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StageData stageData;

    [Header("Options")]
    [SerializeField] private bool startStageOnStart = true;
    [SerializeField] private bool ensureRuntimeHelpers = true;

    private void Awake()
    {
        if (ensureRuntimeHelpers)
        {
            EnsureRuntimeHelpers();
        }

        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();

        if (coreFacade != null)
        {
            coreFacade.ResolveReferences();
            if (uiManager != null)
            {
                uiManager.BindCore(coreFacade);
                coreFacade.SetUIBridge(uiManager);
            }
            coreFacade.WireReferences();
        }
    }

    private void Start()
    {
        if (!startStageOnStart || coreFacade == null)
        {
            return;
        }

        StageData targetStage = stageData != null ? stageData : coreFacade.CurrentStageData;
        if (targetStage == null && coreFacade.stageManager != null)
        {
            targetStage = coreFacade.stageManager.defaultStageData;
        }

        if (targetStage != null)
        {
            coreFacade.LoadStage(targetStage);
        }

        coreFacade.StartStage();
    }

    private void EnsureRuntimeHelpers()
    {
        if (FindObjectOfType<UIManager>() == null)
        {
            GameObject uiObject = new GameObject("RuntimeFeedbackUI");
            uiManager = uiObject.AddComponent<UIManager>();
        }

        if (FindObjectOfType<SimpleFeedbackAudio>() == null)
        {
            GameObject audioObject = new GameObject("RuntimeFeedbackAudio");
            audioObject.AddComponent<SimpleFeedbackAudio>();
        }

        if (FindObjectOfType<NavMeshAgentPlacementFixer>() == null)
        {
            gameObject.AddComponent<NavMeshAgentPlacementFixer>();
        }
    }
}
