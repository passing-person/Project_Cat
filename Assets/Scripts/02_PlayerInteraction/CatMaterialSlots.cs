using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在猫模型节点上（例如 Cat_rigged 下的 Cat.L，或者更上层的 Model 节点）。
/// 会自动扫描身上（含子物体）所有 Renderer 的每个子网格（SubMesh），
/// 生成一份"命名材质插槽"列表：美术只需要在 Inspector 里把材质拖进对应插槽即可实时预览，
/// 不需要理解 Renderer 结构，也不需要碰 Shader Graph。
///
/// 目前 Cat_L 模型只有 3 个 Renderer（身体 / 左眼 / 右眼），所以默认只有 3 个插槽。
/// 如果之后美术在建模软件（Blender）里把身体拆成更多材质槽（耳朵/尾巴/腿……各一个），
/// 重新导入模型后点一下 "Refresh Slots"，这里会自动生成对应数量的新插槽。
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class CatMaterialSlots : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public string label;
        public Renderer renderer;
        public int subMeshIndex;
        public Material material;
    }

    [SerializeField] private List<Slot> slots = new List<Slot>();
    [SerializeField] private bool applyOnAwake = true;

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
                slots.Add(new Slot
                {
                    renderer = renderer,
                    subMeshIndex = i,
                    label = BuildLabel(renderer, i, subMeshCount),
                    material = FindPreviousMaterial(previous, renderer, i) ?? GetCurrentMaterial(renderer, i)
                });
            }
        }
    }

    [ContextMenu("Apply Materials To Renderers")]
    public void Apply()
    {
        if (slots == null)
            return;

        foreach (Slot slot in slots)
        {
            if (slot == null || slot.renderer == null || slot.material == null)
                continue;

            Material[] materials = slot.renderer.sharedMaterials;
            if (slot.subMeshIndex < 0 || slot.subMeshIndex >= materials.Length)
                continue;

            if (materials[slot.subMeshIndex] == slot.material)
                continue;

            materials[slot.subMeshIndex] = slot.material;
            slot.renderer.sharedMaterials = materials;
        }
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

    private static Material FindPreviousMaterial(List<Slot> previous, Renderer renderer, int subMeshIndex)
    {
        if (previous == null)
            return null;

        foreach (Slot slot in previous)
        {
            if (slot != null && slot.renderer == renderer && slot.subMeshIndex == subMeshIndex)
                return slot.material;
        }

        return null;
    }

    private static string BuildLabel(Renderer renderer, int subMeshIndex, int subMeshCount)
    {
        string name = renderer.gameObject.name;
        return subMeshCount > 1 ? $"{name} [{subMeshIndex}]" : name;
    }
}
