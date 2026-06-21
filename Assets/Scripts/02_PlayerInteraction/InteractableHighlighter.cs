using UnityEngine;

public class InteractableHighlighter : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject;

    public void ShowHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(true);
    }

    public void HideHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }
}
