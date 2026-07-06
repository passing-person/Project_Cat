using System.IO;
using UnityEngine;

[ExecuteAlways]
public class OklchColorSpaceSetup : MonoBehaviour
{
    const int LutSize = 33;
    const string RgbToOklabBytesPath = "Assets/Shader/ColorSpace/LUT/Oklch_RgbToOklab_Lut.bytes";
    const string OklabToRgbBytesPath = "Assets/Shader/ColorSpace/LUT/Oklch_OklabToRgb_Lut.bytes";

    static readonly int RgbToOklabLutId = Shader.PropertyToID("_OklchRgbToOklabLut");
    static readonly int OklabToRgbLutId = Shader.PropertyToID("_OklchOklabToRgbLut");
    static readonly int LutParamsId = Shader.PropertyToID("_OklchLutParams");

    static OklchColorSpaceSetup s_Instance;
    static Texture2D s_CachedRgbToOklab;
    static Texture2D s_CachedOklabToRgb;
    static int s_BoundRgbId;
    static int s_BoundOklabId;

    Texture2D m_RgbToOklabLut;
    Texture2D m_OklabToRgbLut;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        BindGlobalsStatic(force: true);
    }

    void OnEnable()
    {
        if (s_Instance != null && s_Instance != this)
        {
            if (IsAutoCreated(s_Instance))
            {
                if (Application.isPlaying)
                    Destroy(s_Instance.gameObject);
                else
                    DestroyImmediate(s_Instance.gameObject);
                s_Instance = null;
            }
            else
            {
                enabled = false;
                return;
            }
        }

        s_Instance = this;
        BindGlobals();
    }

    void OnDisable()
    {
        if (s_Instance == this && !Application.isPlaying)
            s_Instance = null;
    }

    static bool IsAutoCreated(OklchColorSpaceSetup setup)
    {
        return setup.gameObject.hideFlags == HideFlags.HideAndDontSave;
    }

    void OnValidate()
    {
        if (s_Instance == this || s_Instance == null)
            BindGlobals(force: true);
    }

    public void ForceReloadLuts()
    {
        ReleaseRuntimeTextures();
        s_CachedRgbToOklab = null;
        s_CachedOklabToRgb = null;
        s_BoundRgbId = 0;
        s_BoundOklabId = 0;
        BindGlobals(force: true);
    }

    static void EnsureInstance()
    {
        if (s_Instance != null)
            return;

        var existing = FindObjectOfType<OklchColorSpaceSetup>();
        if (existing != null)
        {
            s_Instance = existing;
            existing.BindGlobals();
            return;
        }

        var go = new GameObject("OklchColorSpaceSetup")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        s_Instance = go.AddComponent<OklchColorSpaceSetup>();
    }

    public static void BindGlobalsStatic(bool force = false)
    {
        if (s_Instance != null)
        {
            s_Instance.BindGlobals(force);
            return;
        }

        var existing = FindObjectOfType<OklchColorSpaceSetup>();
        if (existing != null)
        {
            s_Instance = existing;
            existing.BindGlobals(force);
            return;
        }

        if (s_CachedRgbToOklab == null)
            s_CachedRgbToOklab = LoadLutFromBytes(RgbToOklabBytesPath);
        if (s_CachedOklabToRgb == null)
            s_CachedOklabToRgb = LoadLutFromBytes(OklabToRgbBytesPath);

        ApplyGlobals(s_CachedRgbToOklab, s_CachedOklabToRgb, force);
    }

    void BindGlobals(bool force = false)
    {
        if (!IsValidLut(m_RgbToOklabLut))
            m_RgbToOklabLut = s_CachedRgbToOklab ?? LoadLutFromBytes(RgbToOklabBytesPath);
        if (!IsValidLut(m_OklabToRgbLut))
            m_OklabToRgbLut = s_CachedOklabToRgb ?? LoadLutFromBytes(OklabToRgbBytesPath);

        if (m_RgbToOklabLut != null)
            s_CachedRgbToOklab = m_RgbToOklabLut;
        if (m_OklabToRgbLut != null)
            s_CachedOklabToRgb = m_OklabToRgbLut;

        ApplyGlobals(m_RgbToOklabLut, m_OklabToRgbLut, force);
    }

    static void ApplyGlobals(Texture2D rgbToOklab, Texture2D oklabToRgb, bool force)
    {
        int rgbId = rgbToOklab != null ? rgbToOklab.GetInstanceID() : 0;
        int oklabId = oklabToRgb != null ? oklabToRgb.GetInstanceID() : 0;
        if (!force && rgbId == s_BoundRgbId && oklabId == s_BoundOklabId)
            return;

        if (rgbToOklab != null)
            Shader.SetGlobalTexture(RgbToOklabLutId, rgbToOklab);
        if (oklabToRgb != null)
            Shader.SetGlobalTexture(OklabToRgbLutId, oklabToRgb);

        int lutWidth = LutSize * LutSize;
        int lutHeight = LutSize;
        var lutParams = new Vector4(
            1f / lutWidth,
            1f / lutHeight,
            (LutSize - 1f) / lutWidth,
            (LutSize - 1f) / lutHeight);
        Shader.SetGlobalVector(LutParamsId, lutParams);

        s_BoundRgbId = rgbId;
        s_BoundOklabId = oklabId;
    }

    void ReleaseRuntimeTextures()
    {
        if (m_RgbToOklabLut != null)
        {
            if (Application.isPlaying)
                Destroy(m_RgbToOklabLut);
            else
                DestroyImmediate(m_RgbToOklabLut);
            m_RgbToOklabLut = null;
        }

        if (m_OklabToRgbLut != null)
        {
            if (Application.isPlaying)
                Destroy(m_OklabToRgbLut);
            else
                DestroyImmediate(m_OklabToRgbLut);
            m_OklabToRgbLut = null;
        }
    }

    public bool AreLutsReady => IsValidLut(m_RgbToOklabLut) && IsValidLut(m_OklabToRgbLut);

    static bool IsValidLut(Texture2D tex) => tex != null;

    static Texture2D LoadLutFromBytes(string bytesPath)
    {
        byte[] rawBytes = ReadBytesAsset(bytesPath);
        if (rawBytes == null)
        {
            Debug.LogError($"OKLCH LUT bytes not found: {bytesPath}");
            return null;
        }

        int lutWidth = LutSize * LutSize;
        int lutHeight = LutSize;
        int expectedBytes = lutWidth * lutHeight * 16;
        if (rawBytes.Length != expectedBytes)
        {
            Debug.LogError($"OKLCH LUT bytes size mismatch for {bytesPath}: got {rawBytes.Length}, expected {expectedBytes}.");
            return null;
        }

        var tex = new Texture2D(lutWidth, lutHeight, TextureFormat.RGBAFloat, false, true)
        {
            name = Path.GetFileNameWithoutExtension(bytesPath),
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.LoadRawTextureData(rawBytes);
        tex.Apply(false, true);
        return tex;
    }

    static byte[] ReadBytesAsset(string bytesPath)
    {
#if UNITY_EDITOR
        var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(bytesPath);
        if (textAsset != null && textAsset.bytes != null && textAsset.bytes.Length > 0)
            return textAsset.bytes;

        string absolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            bytesPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
            return File.ReadAllBytes(absolutePath);
#endif

        string resourceName = Path.GetFileNameWithoutExtension(bytesPath);
        var bytesAsset = Resources.Load<TextAsset>($"ColorSpace/{resourceName}");
        if (bytesAsset != null && bytesAsset.bytes != null && bytesAsset.bytes.Length > 0)
            return bytesAsset.bytes;

        string streamingPath = Path.Combine(Application.streamingAssetsPath, "ColorSpace", resourceName + ".bytes");
        if (File.Exists(streamingPath))
            return File.ReadAllBytes(streamingPath);

        return null;
    }
}
