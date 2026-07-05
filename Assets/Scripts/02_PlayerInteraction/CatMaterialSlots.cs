using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在猫模型节点上（例如 Cat_rigged 下的 Cat.L，或者更上层的 Model 节点）。
/// 会自动扫描身上（含子物体）所有 Renderer 的每个子网格（SubMesh），
/// 生成一份"命名材质插槽"列表：美术只需要在 Inspector 里把材质拖进对应插槽即可实时预览，
/// 不需要理解 Renderer 结构，也不需要碰 Shader Graph。
///
/// 每个插槽还额外暴露了 Specular / Smoothness / Emission 三个滑条，
/// 对应 LowPoly_Base 这套 Shader 里同名的属性（反光强度 / 光滑度 / 自发光）。
/// 这三个值通过 MaterialPropertyBlock 应用，只影响这一个 Renderer 的这一个子网格，
/// 不会写回 .mat 材质文件本身，所以可以放心地按每个部位单独调（比如让眼睛比身体更亮/更有光泽），
/// 也不用担心改坏了共用材质、影响到其他场景或其他猫。
///
/// 目前 Cat_L 模型只有 3 个 Renderer（身体 / 左眼 / 右眼），所以默认只有 3 个插槽。
/// 如果之后美术在建模软件（Blender）里把身体拆成更多材质槽（耳朵/尾巴/腿……各一个），
/// 重新导入模型后点一下 "Refresh Slots"，这里会自动生成对应数量的新插槽。
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class CatMaterialSlots : MonoBehaviour
{
    // LowPoly_Base.shadergraph 里这三个属性的内部引用名（Shader Graph 自动生成，不能改名字）。
    private const string SpecularPropertyName = "Vector1_F0325914";
    private const string SmoothnessPropertyName = "Vector1_EE791F2B";
    private const string EmissionPropertyName = "Vector1_851457A2";

    private static readonly int SpecularPropertyId = Shader.PropertyToID(SpecularPropertyName);
    private static readonly int SmoothnessPropertyId = Shader.PropertyToID(SmoothnessPropertyName);
    private static readonly int EmissionPropertyId = Shader.PropertyToID(EmissionPropertyName);

    [Serializable]
    public class Slot
    {
        public string label;
        public Renderer renderer;
        public int subMeshIndex;
        public Material material;

        [Header("Shader 反光 / 自发光（不写入材质文件，仅本对象生效）")]
        public bool overrideShaderValues;
        [Range(0f, 1f)] public float specular;
        [Range(0f, 1f)] public float smoothness;
        [Range(0f, 1f)] public float emission;
    }

    [SerializeField] private List<Slot> slots = new List<Slot>();
    [SerializeField] private bool applyOnAwake = true;

    private static MaterialPropertyBlock sharedBlock;

    public IReadOnlyList<Slot> Slots => slots;

    private void Reset()
    {
        RefreshSlots();
    }

    private void Awake()
    {
        if (applyOnAwake)
            Apply();
    }

    private void OnValidate()
    {
        if (slots == null || slots.Count == 0)
            RefreshSlots();

        Apply();
    }

    [ContextMenu("Refresh Slots")]
    public void RefreshSlots()
    {
        List<Slot> previous = slots;
        slots = new List<Slot>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            int subMeshCount = GetSubMeshCount(renderer);
            for (int i = 0; i < subMeshCount; i++)
            {
                Slot previousSlot = FindPreviousSlot(previous, renderer, i);
                Material material = previousSlot != null ? previousSlot.material : GetCurrentMaterial(renderer, i);

                slots.Add(new Slot
                {
                    renderer = renderer,
                    subMeshIndex = i,
                    label = BuildLabel(renderer, i, subMeshCount),
                    material = material,
                    overrideShaderValues = previousSlot != null && previousSlot.overrideShaderValues,
                    specular = previousSlot != null ? previousSlot.specular : GetMaterialFloat(material, SpecularPropertyName, 0f),
                    smoothness = previousSlot != null ? previousSlot.smoothness : GetMaterialFloat(material, SmoothnessPropertyName, 0f),
                    emission = previousSlot != null ? previousSlot.emission : GetMaterialFloat(material, EmissionPropertyName, 0f)
                });
            }
        }
    }

    [ContextMenu("Apply Materials To Renderers")]
    public void Apply()
    {
        if (slots == null)
            return;

        if (sharedBlock == null)
            sharedBlock = new MaterialPropertyBlock();

        foreach (Slot slot in slots)
        {
            if (slot == null || slot.renderer == null)
                continue;

            ApplyMaterial(slot);
            ApplyShaderOverrides(slot);
        }
    }

    private static void ApplyMaterial(Slot slot)
    {
        if (slot.material == null)
            return;

        Material[] materials = slot.renderer.sharedMaterials;
        if (slot.subMeshIndex < 0 || slot.subMeshIndex >= materials.Length)
            return;

        if (materials[slot.subMeshIndex] == slot.material)
            return;

        materials[slot.subMeshIndex] = slot.material;
        slot.renderer.sharedMaterials = materials;
    }

    private static void ApplyShaderOverrides(Slot slot)
    {
        if (!slot.overrideShaderValues)
        {
            slot.renderer.SetPropertyBlock(null, slot.subMeshIndex);
            return;
        }

        slot.renderer.GetPropertyBlock(sharedBlock, slot.subMeshIndex);
        sharedBlock.SetFloat(SpecularPropertyId, slot.specular);
        sharedBlock.SetFloat(SmoothnessPropertyId, slot.smoothness);
        sharedBlock.SetFloat(EmissionPropertyId, slot.emission);
        slot.renderer.SetPropertyBlock(sharedBlock, slot.subMeshIndex);
    }

    private static int GetSubMeshCount(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            return Mathf.Max(1, skinned.sharedMesh.subMeshCount);

        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            return Mathf.Max(1, filter.sharedMesh.subMeshCount);

        return Mathf.Max(1, renderer.sharedMaterials.Length);
    }

    private static Material GetCurrentMaterial(Renderer renderer, int subMeshIndex)
    {
        Material[] materials = renderer.sharedMaterials;
        return subMeshIndex < materials.Length ? materials[subMeshIndex] : null;
    }

    private static float GetMaterialFloat(Material material, string propertyName, float fallback)
    {
        if (material != null && material.HasProperty(propertyName))
            return material.GetFloat(propertyName);

        return fallback;
    }

    private static Slot FindPreviousSlot(List<Slot> previous, Renderer renderer, int subMeshIndex)
    {
        if (previous == null)
            return null;

        foreach (Slot slot in previous)
        {
            if (slot != null && slot.renderer == renderer && slot.subMeshIndex == subMeshIndex)
                return slot;
        }

        return null;
    }

    private static string BuildLabel(Renderer renderer, int subMeshIndex, int subMeshCount)
    {
        string name = renderer.gameObject.name;
        return subMeshCount > 1 ? $"{name} [{subMeshIndex}]" : name;
    }
}
