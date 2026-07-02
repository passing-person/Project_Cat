using UnityEngine;

public class AnimatorDebugProbe : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (animator == null)
            return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        Debug.Log(
            $"Animator: {animator.name}, " +
            $"StateHash: {info.shortNameHash}, " +
            $"NormalizedTime: {info.normalizedTime:F2}, " +
            $"Speed: {animator.speed}, " +
            $"AvatarValid: {(animator.avatar != null && animator.avatar.isValid)}"
        );
    }
}