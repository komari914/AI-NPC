using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject endPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI messageText;

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button nextScenarioButton;

    [Header("Colors")]
    public Color successColor = Color.green;
    public Color failureColor = Color.red;

    void Start()
    {
        HideGameEnd();
        SetupButtons();

        // Subscribe to case resolution
        if (CaseProgressManager.Instance != null)
        {
            // Check for resolution periodically (could also use events)
            InvokeRepeating("CheckCaseResolution", 1f, 1f);
        }
    }

    void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartScenario);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (nextScenarioButton != null)
        {
            nextScenarioButton.onClick.AddListener(NextScenario);
        }
    }

    void CheckCaseResolution()
    {
        if (CaseProgressManager.Instance == null) return;
        if (TimerManager.Instance == null) return;

        // If case is resolved and timer is still running, show success
        if (CaseProgressManager.Instance.phase == CasePhase.Resolved &&
            endPanel != null && !endPanel.activeSelf)
        {
            float timeUsed = TimerManager.Instance.timeElapsed;
            ShowGameEnd(true, timeUsed, "Case solved! Well done, investigator.");

            // Stop timer
            if (TimerManager.Instance.isRunning)
            {
                TimerManager.Instance.PauseTimer();
            }
        }
    }

    /// <summary>
    /// Show game end screen
    /// </summary>
    /// <param name="success">Whether the player succeeded</param>
    /// <param name="timeUsed">Time used in seconds</param>
    /// <param name="message">Additional message</param>
    public void ShowGameEnd(bool success, float timeUsed, string message = "")
    {
        if (endPanel == null) return;

        endPanel.SetActive(true);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player controls
        DisablePlayerControls();

        // Set result text
        if (resultText != null)
        {
            resultText.text = success ? "SUCCESS" : "CASE UNSOLVED";
            resultText.color = success ? successColor : failureColor;
        }

        // Set time text
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeUsed / 60f);
            int seconds = Mathf.FloorToInt(timeUsed % 60f);
            timeText.text = $"Time Used: {minutes:00}:{seconds:00}";
        }

        // Set message text
        if (messageText != null)
        {
            messageText.text = message;
        }

        // Update button visibility
        UpdateButtonVisibility();

        // Save experiment data
        if (DataRecorder.Instance != null)
        {
            DataRecorder.Instance.EndSession(saveData: true);
            DataRecorder.Instance.ExportToCSV();
            Debug.Log($"[GameEnd] Experiment data saved to: {DataRecorder.Instance.GetSaveDirectoryPath()}");
        }

        Debug.Log($"[GameEnd] Showing end screen - Success: {success}, Time: {timeUsed:F1}s");
    }

    /// <summary>
    /// Hide game end screen
    /// </summary>
    public void HideGameEnd()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }
    }

    void UpdateButtonVisibility()
    {
        // Show next scenario button only if not on scenario 4
        if (nextScenarioButton != null && ScenarioManager.Instance != null)
        {
            int currentScenario = ScenarioManager.Instance.scenarioIndex;
            nextScenarioButton.gameObject.SetActive(currentScenario < 4);
        }
    }

    void RestartScenario()
    {
        Debug.Log("[GameEnd] Restarting scenario");

        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.RestartScenario();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    void ReturnToMainMenu()
    {
        Debug.Log("[GameEnd] Returning to main menu");

        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.ReturnToMainMenu();
        }
        else
        {
            // Fallback: try to load MainMenu scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    void NextScenario()
    {
        Debug.Log("[GameEnd] Loading next scenario");

        if (ScenarioManager.Instance != null)
        {
            int nextIndex = ScenarioManager.Instance.scenarioIndex + 1;
            if (nextIndex <= 4)
            {
                ScenarioManager.Instance.StartScenarioByIndex(nextIndex);
            }
            else
            {
                Debug.Log("[GameEnd] No more scenarios, returning to main menu");
                ReturnToMainMenu();
            }
        }
    }

    void DisablePlayerControls()
    {
        // Disable FPS controller
        SimpleFPSController fps = FindObjectOfType<SimpleFPSController>();
        if (fps != null) fps.enabled = false;

        // Disable player interaction
        PlayerInteraction interaction = FindObjectOfType<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;

        // Hide dialogue UI if open
        DialogueInputUI dialogueUI = FindObjectOfType<DialogueInputUI>();
        if (dialogueUI != null) dialogueUI.Hide();
    }

    void OnDestroy()
    {
        CancelInvoke("CheckCaseResolution");
    }
}
