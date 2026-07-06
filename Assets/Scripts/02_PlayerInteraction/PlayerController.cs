using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private string playerId = "PlayerCat";
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    private bool baseControllable = true;
    private int temporaryControlLockCount;

    public string PlayerId => playerId;
    public bool IsHidden { get; private set; }
    public bool IsControllable => baseControllable && temporaryControlLockCount <= 0;
    public bool IsBaseControllable => baseControllable;
    public bool HasTemporaryControlLock => temporaryControlLockCount > 0;

    public void SetControllable(bool value)
    {
        baseControllable = value;
    }

    public void AddTemporaryControlLock()
    {
        temporaryControlLockCount++;
    }

    public void RemoveTemporaryControlLock()
    {
        temporaryControlLockCount = Mathf.Max(0, temporaryControlLockCount - 1);
    }

    public void SetHidden(bool value)
    {
        IsHidden = value;
    }

    public void PlayCaught()
    {
        SetControllable(false);
        SetHidden(false);
        animationController?.PlayCaught();
        sfxController?.PlayCaught();
    }

    public PlayerContext GetContext()
    {
        return new PlayerContext(PlayerId, transform.position, IsHidden, IsControllable);
    }
}
