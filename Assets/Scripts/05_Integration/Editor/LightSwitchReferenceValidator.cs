#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LightSwitchReferenceValidator
{
    [MenuItem("Tools/Project Cat/MainScene/Validate Light Switch References")]
    public static void ValidateLightSwitchReferences()
    {
        MischiefWorldEventReporter[] reporters = Object.FindObjectsOfType<MischiefWorldEventReporter>();
        int checkedCount = 0;
        int invalidCount = 0;

        for (int i = 0; i < reporters.Length; i++)
        {
            MischiefWorldEventReporter reporter = reporters[i];
            if (reporter == null)
            {
                continue;
            }

            if (!reporter.ValidateLightSetup(out string report))
            {
                invalidCount++;
                Debug.LogWarning("Light switch setup invalid: " + report, reporter);
            }
            else if (report.Contains("direct Light references"))
            {
                checkedCount++;
                Debug.Log("Light switch setup valid: " + report, reporter);
            }
        }

        Debug.Log($"Light switch validation complete. Valid light switches: {checkedCount}, invalid: {invalidCount}.");
    }
}
#endif
