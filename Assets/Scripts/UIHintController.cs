using TMPro;
using UnityEngine;

public class UIHintController : MonoBehaviour
{
    [Tooltip("Drag the TextMeshProUGUI component here (InteractHintText).")]
    public TextMeshProUGUI hintText;

    void Awake()
    {
        // Try auto-find if not assigned
        if (hintText == null)
            hintText = GetComponent<TextMeshProUGUI>();

        // If still null, try find in children (in case script is on parent)
        if (hintText == null)
            hintText = GetComponentInChildren<TextMeshProUGUI>();

        // Fail gracefully (no crash)
        if (hintText == null)
        {
            Debug.LogError("[UIHintController] No TextMeshProUGUI found. " +
                           "Attach this script to a TMP Text object, or assign hintText in Inspector.");
            return;
        }

        Hide();
    }

    public void Show(string message)
    {
        if (hintText == null) return;

        hintText.text = message;
        hintText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (hintText == null) return;

        hintText.gameObject.SetActive(false);
    }
}