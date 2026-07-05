using UnityEngine;

public class TestRageReceiverRegistrar : MonoBehaviour
{
    [SerializeField] private MockRageReceiver receiver;
    [SerializeField] private RageManager rageManager;

    private void Awake()
    {
        if (receiver == null)
            receiver = GetComponent<MockRageReceiver>();

        if (rageManager == null)
            rageManager = FindObjectOfType<RageManager>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void Start()
    {
        Register();
    }

    private void OnDisable()
    {
        if (rageManager != null && receiver != null)
            rageManager.UnregisterNpc(receiver.NpcId);
    }

    private void Register()
    {
        if (rageManager == null)
            rageManager = FindObjectOfType<RageManager>();

        if (receiver == null)
            receiver = GetComponent<MockRageReceiver>();

        if (rageManager != null && receiver != null)
            rageManager.RegisterNpc(receiver);
    }
}
