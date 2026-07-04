using UnityEngine;

public class AverySceneStarter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        gameManager?.StartGame();
    }
}
