using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int MischiefHash = Animator.StringToHash("Mischief");
    private static readonly int CuteHash = Animator.StringToHash("Cute");
    private static readonly int HideHash = Animator.StringToHash("Hide");
    private static readonly int CaughtHash = Animator.StringToHash("Caught");
    private static readonly int TurnLeftHash = Animator.StringToHash("TurnLeft");
    private static readonly int TurnRightHash = Animator.StringToHash("TurnRight");

    [Header("Model")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Animator animator;

    [Header("Root Motion Guard")]
    [SerializeField] private bool lockAnimatedRootPosition = true;
    [SerializeField] private Transform animatedRoot;

    private Vector3 initialAnimatedRootLocalPosition;

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform.Find(PlayerBodySetup.ModelChildName);

        if (animator == null && modelRoot != null)
            animator = modelRoot.GetComponent<Animator>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animatedRoot == null)
            animatedRoot = FindAnimatedRoot();

        if (animatedRoot != null)
            initialAnimatedRootLocalPosition = animatedRoot.localPosition;
    }

    private void LateUpdate()
    {
        if (!lockAnimatedRootPosition || animatedRoot == null)
            return;

        animatedRoot.localPosition = initialAnimatedRootLocalPosition;
    }

    public void SetMoveSpeed(float speed)
    {
        if (animator == null)
            return;

        animator.SetFloat(MoveSpeedHash, speed);
    }

    public void SetMoveDirection(float x, float y)
    {
        if (animator == null)
            return;

        animator.SetFloat(MoveXHash, x, 0.08f, Time.deltaTime);
        animator.SetFloat(MoveYHash, y, 0.08f, Time.deltaTime);
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

    public void PlayTurnLeft()
    {
        if (animator == null)
            return;

        animator.SetTrigger(TurnLeftHash);
    }

    public void PlayTurnRight()
    {
        if (animator == null)
            return;

        animator.SetTrigger(TurnRightHash);
    }

    private Transform FindAnimatedRoot()
    {
        Transform searchRoot = animator != null ? animator.transform : transform;
        Transform root = searchRoot.Find("Root");
        if (root != null)
            return root;

        return FindChildRecursive(searchRoot, "Root");
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }
}
