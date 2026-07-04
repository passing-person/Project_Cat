using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int MischiefHash = Animator.StringToHash("Mischief");
    private static readonly int CuteHash = Animator.StringToHash("Cute");
    private static readonly int HideHash = Animator.StringToHash("Hide");
    private static readonly int CaughtHash = Animator.StringToHash("Caught");

    [Header("Model")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform.Find(PlayerBodySetup.ModelChildName);

        if (animator == null && modelRoot != null)
            animator = modelRoot.GetComponent<Animator>();
    }

    public void SetMoveSpeed(float speed)
    {
        if (animator == null)
            return;

        animator.SetFloat(MoveSpeedHash, speed);
    }

    public void SetGrounded(bool grounded)
    {
        if (animator == null)
            return;

        animator.SetBool(IsGroundedHash, grounded);
    }

    public void PlayJump()
    {
        if (animator == null)
            return;

        animator.SetTrigger(JumpHash);
    }

    public void PlayMischief()
    {
        if (animator == null)
            return;

        animator.SetTrigger(MischiefHash);
    }

    public void PlayCute()
    {
        if (animator == null)
            return;

        animator.SetTrigger(CuteHash);
    }

    public void PlayHide(bool isHidden)
    {
        if (animator == null)
            return;

        animator.SetBool(HideHash, isHidden);
    }

    public void PlayCaught()
    {
        if (animator == null)
            return;

        animator.SetTrigger(CaughtHash);
    }
}
