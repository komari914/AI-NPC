using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shows a popup panel when player inspects an evidence item.
/// Assign this component to a UI Panel GameObject.
///
/// Required UI structure:
///   EvidencePopupPanel
///     ├── TitleText      (TextMeshProUGUI)
///     ├── BodyText       (TextMeshProUGUI)
///     └── CloseButton    (Button)  ← optional, player can also press E or Esc
/// </summary>
public class EvidencePopupUI : MonoBehaviour
{
    public static EvidencePopupUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Button closeButton;

    [Header("Controls")]
    public SimpleFPSController fpsController;
    public PlayerInteraction playerInteraction;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel?.SetActive(false);
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!IsOpen) return;

        // Close on E or Escape
        if (Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    /// <summary>Called by EvidenceInteractable after inspection.</summary>
    public void Show(string evidenceId, string description)
    {
        if (panel == null) return;

        // Parse a cleaner title from the id, e.g. "A3" → "Evidence A3"
        titleText.text = $"Evidence — {evidenceId}";
        bodyText.text  = description;

        panel.SetActive(true);

        // Pause player movement / interaction
        if (fpsController != null) fpsController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Close()
    {
        panel?.SetActive(false);

        if (fpsController != null) fpsController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
