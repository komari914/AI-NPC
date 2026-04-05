using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pause menu triggered by Escape key.
/// Pauses the timer and disables player controls while open.
///
/// Required UI structure:
///   PausePanel
///     ├── ResumeButton    (Button)
///     ├── MainMenuButton  (Button)
///     └── QuitButton      (Button)      ← optional
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Player Controls to disable")]
    public SimpleFPSController fpsController;
    public PlayerInteraction playerInteraction;
    public DialogueInputUI dialogueInputUI;

    private bool isPaused = false;

    void Start()
    {
        pausePanel?.SetActive(false);

        resumeButton?.onClick.AddListener(Resume);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
        quitButton?.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Don't allow pause if evidence popup, dialogue input, or game end is open
        if (EvidencePopupUI.Instance  != null && EvidencePopupUI.Instance.IsOpen)  return;
        if (DialogueInputUI.Instance  != null && DialogueInputUI.Instance.IsOpen)  return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else          Pause();
        }
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        pausePanel?.SetActive(true);

        // Pause timer
        TimerManager.Instance?.PauseTimer();

        // Disable controls
        if (fpsController != null) fpsController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        dialogueInputUI?.Hide();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        pausePanel?.SetActive(false);

        // Resume timer
        TimerManager.Instance?.ResumeTimer();

        // Re-enable controls
        if (fpsController != null) fpsController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void GoToMainMenu()
    {
        // Save data before leaving
        DataRecorder.Instance?.EndSession();

        Time.timeScale = 1f;
        isPaused = false;

        // No main menu — reload the current scenario from scratch
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void QuitGame()
    {
        DataRecorder.Instance?.EndSession();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
