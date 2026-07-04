using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcRageUI : MonoBehaviour
{
    [SerializeField] private string npcId;
    [SerializeField] private Slider rageSlider;
    [SerializeField] private TMP_Text rageText;
    [SerializeField] private TMP_Text stateText;

    public string NpcId => npcId;

    public void SetRage(float rage, NpcRageState state)
    {
        if (rageSlider != null)
            rageSlider.value = rage / 100f;

        if (rageText != null)
            rageText.text = $"{Mathf.RoundToInt(rage)}%";

        if (stateText != null)
            stateText.text = state.ToString();
    }
}
