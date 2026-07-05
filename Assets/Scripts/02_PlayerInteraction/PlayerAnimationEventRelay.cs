using UnityEngine;

/// <summary>
/// 挂在 Animator 所在的节点上（视觉层，即 "Model"）。
/// Unity 的 Animation Event 只会 SendMessage 给 Animator 所在的那个 GameObject，
/// 够不到父级（玩法根节点）上的脚本，所以在这里统一转发。
/// 用 SendMessageUpwards 而不是写死某个具体脚本类型，
/// 这样以后不管外层挂的是 TrashCubeSpawner 还是别的脚本，
/// 只要方法名对得上，都能收到，不需要改这个中转脚本。
/// 新增动画事件方法时，在这里加一行同名转发即可。
/// </summary>
public class PlayerAnimationEventRelay : MonoBehaviour
{
    // 由 A_Cat_Action 动画片段里的 Animation Event 调用。
    public void AE_SpawnTrashCubes()
    {
        RelayToParent(nameof(AE_SpawnTrashCubes));
    }

    public void AE_StopTrashCubes()
    {
        RelayToParent(nameof(AE_StopTrashCubes));
    }

    private void RelayToParent(string methodName)
    {
        if (transform.parent == null)
            return;

        transform.parent.SendMessageUpwards(methodName, SendMessageOptions.DontRequireReceiver);
    }
}
