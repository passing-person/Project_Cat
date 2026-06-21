using UnityEngine;

public readonly struct PlayerContext
{
    public readonly string PlayerId;
    public readonly Vector3 Position;
    public readonly bool IsHidden;
    public readonly bool IsControllable;

    public PlayerContext(string playerId, Vector3 position, bool isHidden, bool isControllable)
    {
        PlayerId = playerId;
        Position = position;
        IsHidden = isHidden;
        IsControllable = isControllable;
    }
}
