using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Press TAB to open/close the Evidence Journal.
///
/// Directory layout: each evidence button is placed at a fixed anchored position
/// defined in the Inspector via the "slots" array.  No ScrollView needed —
/// just a plain RectTransform panel as directoryContent.
///
/// Required UI hierarchy (script on a PARENT that is ALWAYS active):
///
///   EvidenceJournalController        ← this script
///     └── JournalPanel
///           ├── DirectoryPanel
///           │     └── DirectoryContent   ← plain RectTransform, no LayoutGroup
///           └── DetailPanel
///                 ├── DetailTitleText    (TextMeshProUGUI)
///                 ├── DetailBodyText     (TextMeshProUGUI)
///                 └── BackButton         (Button)
///
/// Slot positions use Unity anchored-position coordinates
/// (origin = centre of directoryContent).  Example values:
///   A1  (-350,  150)  ← top-left
///   A2  (   0,  150)  ← top-centre
///   A3  ( 350,  150)  ← top-right
///   P1  (-350,    0)  ← middle-left
///   J1  (-350, -150)  ← bottom-left
/// </summary>
public class EvidenceJournalUI : MonoBehaviour
{
    // ── Per-evidence position slot (Inspector-editable) ──────────────────────
    [System.Serializable]
    public struct EvidenceSlot
    {
        public string  evidenceId;
        public Vector2 anchoredPosition;   // position inside DirectoryContent
    }

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Panels")]
    public GameObject journalPanel;
    public GameObject directoryPanel;
    public GameObject detailPanel;

    [Header("Directory")]
    [Tooltip("Plain RectTransform panel — no LayoutGroup. Buttons are placed by slot positions.")]
    public RectTransform directoryContent;   // parent for spawned buttons
    public Button        entryButtonPrefab;  // root must have (or child has) TMP
    public Vector2       buttonSize = new Vector2(200f, 60f);

    [Tooltip("Define where each evidence ID appears in the directory.")]
    public EvidenceSlot[] slots;

    [Header("Detail")]
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailBodyText;
    public Button          backButton;

    [Header("Controls")]
    public SimpleFPSController fpsController;
    public PlayerInteraction   playerInteraction;

    // ─── Runtime state ────────────────────────────────────────────────────────

    // id → (displayName, description)
    private readonly Dictionary<string, (string name, string desc)> entries = new();

    private bool isOpen = false;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        journalPanel?.SetActive(false);
        if (backButton != null)
            backButton.onClick.AddListener(ShowDirectory);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (DialogueInputUI.Instance != null && DialogueInputUI.Instance.IsOpen) return;

        bool tabPressed    = Keyboard.current.tabKey.wasPressedThisFrame;
        bool escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;

        if (tabPressed || (escapePressed && isOpen))
        {
            if (EvidencePopupUI.Instance != null && EvidencePopupUI.Instance.IsOpen) return;
            if (isOpen) Close();
            else        Open();
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called when evidence is collected.</summary>
    public void AddEntry(string evidenceId, string description, string displayName = "")
    {
        if (string.IsNullOrEmpty(evidenceId)) return;
        entries[evidenceId] = (displayName, description);   // overwrite = fine
    }

    // ─── Open / Close ─────────────────────────────────────────────────────────

    void Open()
    {
        if (journalPanel == null) { Debug.LogError("[Journal] journalPanel not assigned!"); return; }
        isOpen = true;
        journalPanel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(null); // prevent Tab being eaten by UI navigation
        ShowDirectory();

        if (fpsController     != null) fpsController.enabled     = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void Close()
    {
        isOpen = false;
        journalPanel?.SetActive(false);

        if (fpsController     != null) fpsController.enabled     = true;
        if (playerInteraction != null) playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ─── Directory panel ──────────────────────────────────────────────────────

    void ShowDirectory()
    {
        if (directoryPanel != null) directoryPanel.SetActive(true);
        if (detailPanel    != null) detailPanel.SetActive(false);
        RebuildDirectory();
    }

    void RebuildDirectory()
    {
        if (directoryContent == null)
        {
            Debug.LogError("[Journal] directoryContent not assigned!");
            return;
        }

        // Remove previously spawned buttons
        foreach (Transform child in directoryContent)
            Destroy(child.gameObject);

        if (entries.Count == 0)
        {
            SpawnPlaceholder("No evidence collected yet.");
            return;
        }

        // Build a lookup: evidenceId → slot position
        var positionMap = new Dictionary<string, Vector2>();
        if (slots != null)
            foreach (var s in slots)
                if (!string.IsNullOrEmpty(s.evidenceId))
                    positionMap[s.evidenceId] = s.anchoredPosition;

        foreach (var kv in entries)
        {
            string id   = kv.Key;
            string name = kv.Value.name;
            string desc = kv.Value.desc;

            if (entryButtonPrefab == null) break;

            Button btn = Instantiate(entryButtonPrefab, directoryContent);

            // Size
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = buttonSize;

            // Position — use slot if defined, otherwise stack vertically as fallback
            if (positionMap.TryGetValue(id, out Vector2 pos))
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
            }

            // Label: "A1  Marked Design Draft"
            string label = string.IsNullOrEmpty(name) ? id : $"{id}  {name}";
            SetButtonLabel(btn, label);

            // Click → detail page
            string capturedId   = id;
            string capturedName = name;
            string capturedDesc = desc;
            btn.onClick.AddListener(() => ShowDetail(capturedId, capturedName, capturedDesc));
        }
    }

    void SpawnPlaceholder(string message)
    {
        if (entryButtonPrefab == null) return;
        Button btn = Instantiate(entryButtonPrefab, directoryContent);
        btn.interactable = false;
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.sizeDelta        = buttonSize;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        SetButtonLabel(btn, message);
    }

    // ─── Detail panel ─────────────────────────────────────────────────────────

    void ShowDetail(string id, string name, string desc)
    {
        if (directoryPanel != null) directoryPanel.SetActive(false);
        if (detailPanel    != null) detailPanel.SetActive(true);

        if (detailTitleText != null)
            detailTitleText.text = string.IsNullOrEmpty(name) ? id : $"[{id}]  {name}";

        if (detailBodyText != null)
            detailBodyText.text = desc;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static void SetButtonLabel(Button btn, string text)
    {
        var tmp = btn.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }
}
