using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeadLookAt : MonoBehaviour
{
    [SerializeField] Transform lookTarget;

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (lookTarget == null) return;

        animator.SetLookAtWeight(1f, 0.1f, 0.9f, 1f, 0.5f);
        animator.SetLookAtPosition(lookTarget.position);
    }
}