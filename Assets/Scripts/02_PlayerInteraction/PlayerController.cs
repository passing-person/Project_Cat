using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private string playerId = "PlayerCat";
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    public string PlayerId => playerId;
    public bool IsHidden { get; private set; }
    public bool IsControllable { get; private set; } = true;

    public void SetControllable(bool value)
    {
        IsControllable = value;
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
