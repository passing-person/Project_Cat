using UnityEngine;

/// <summary>
/// 挂在 PlayerCat/Model 上，标记此节点仅用于显示模型（无碰撞体、无物理）。
/// </summary>
[DisallowMultipleComponent]
public class PlayerModel : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    public MeshFilter MeshFilter => meshFilter;
    public MeshRenderer MeshRenderer => meshRenderer;

    private void Reset()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnValidate()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }
}
