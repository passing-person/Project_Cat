using UnityEngine;

public class MainSceneStarter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StageData stageData;

    [Header("Options")]
    [SerializeField] private bool startStageOnStart = true;

    private void Awake()
    {
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
}
