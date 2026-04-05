using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a controls reference panel at scene start.
/// The opening NPC sequence is held until the player dismisses this panel.
///
/// UI structure:
///   ControlsPanel
///     ├── ControlsText   (TextMeshProUGUI)
///     └── StartButton    (Button)  "Got it — Start"
///
/// Assign this script to a parent GameObject that is always active.
/// Assign MentorNPC so opening is delayed until panel is dismissed.
/// </summary>
public class ControlsTutorialUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject      controlsPanel;
    public TextMeshProUGUI controlsText;
    public Button          startButton;

    [Header("References")]
    public MentorNPC mentorNPC;

    [Header("Controls")]
    public SimpleFPSController  fpsController;
    public PlayerInteraction    playerInteraction;

    // ─── Text templates ──────────────────────────────────────────────────────

    private const string TextModeControls =
@"<b>Movement</b>
  WASD           Move
  Mouse          Look around

<b>Interaction</b>
  E              Inspect evidence / Confirm
  F              Talk to Mentor

<b>Journal</b>
  TAB            Open / Close evidence journal

<b>Menu</b>
  ESC            Pause";

    private const string VoiceModeControls =
@"<b>Movement</b>
  WASD           Move
  Mouse          Look around

<b>Interaction</b>
  E              Inspect evidence
  V (hold)       Speak to Mentor

<b>Journal</b>
  TAB            Open / Close evidence journal

<b>Menu</b>
  ESC            Pause";

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        // Prevent MentorNPC from starting the opening until we dismiss
        if (mentorNPC != null)
            mentorNPC.playOpeningOnStart = false;

        // Freeze player controls while panel is shown
        if (fpsController    != null) fpsController.enabled    = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        // Show correct controls for this modality
        if (controlsText != null)
        {
            bool isVoice = ScenarioManager.Instance != null &&
                           ScenarioManager.Instance.modality == ModalityType.Voice;
            controlsText.text = isVoice ? VoiceModeControls : TextModeControls;
        }

        controlsPanel?.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        startButton?.onClick.AddListener(OnStartClicked);
    }

    // ─── Button handler ───────────────────────────────────────────────────────

    void OnStartClicked()
    {
        controlsPanel?.SetActive(false);

        // Restore player controls
        if (fpsController    != null) fpsController.enabled    = true;
        if (playerInteraction != null) playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Now start the opening NPC sequence
        if (mentorNPC != null)
            mentorNPC.StartOpeningManually();
    }
}
