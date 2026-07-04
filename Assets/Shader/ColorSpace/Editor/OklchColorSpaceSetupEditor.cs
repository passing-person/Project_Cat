#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class OklchColorSpaceSetupEditor
{
    static OklchColorSpaceSetupEditor()
    {
        EditorApplication.delayCall += BindLutsInEditor;
        EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += BindLutsInEditor;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += BindLutsInEditor;
        EditorSceneManager.sceneClosed += _ => EditorApplication.delayCall += BindLutsInEditor;
    }

    static void BindLutsInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        OklchColorSpaceSetup.BindGlobalsStatic(force: true);
    }
}

[CustomEditor(typeof(OklchColorSpaceSetup))]
class OklchColorSpaceSetupInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var setup = (OklchColorSpaceSetup)target;

        EditorGUILayout.HelpBox(
            "LUT 纹理从 Assets/Shader/ColorSpace/LUT/*.bytes 自动加载，无需手动指定。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("LUT Ready", setup.AreLutsReady);
        }

        if (GUILayout.Button("Reload LUTs"))
            setup.ForceReloadLuts();

        if (!setup.AreLutsReady)
            EditorGUILayout.HelpBox("LUT 未加载成功，请确认 .bytes 文件存在于 LUT 文件夹。", MessageType.Warning);
    }
}
#endif
