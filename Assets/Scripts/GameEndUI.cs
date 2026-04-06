using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject      endPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI surveyText;

    [Header("Buttons")]
    public Button nextScenarioButton;

    [Header("Colors")]
    public Color successColor = Color.green;
    public Color failureColor = Color.red;

    [Header("Survey Reminder")]
    [Tooltip("Shown between scenarios")]
    [TextArea(2, 4)]
    public string surveyMessageMid  = "Please complete the survey for this session before clicking Next Scenario.";
    [Tooltip("Shown after the final scenario")]
    [TextArea(2, 4)]
    public string surveyMessageFinal = "Please complete the final survey before closing the browser. Thank you for participating!";

    void Start()
    {
        HideGameEnd();

        if (nextScenarioButton != null)
            nextScenarioButton.onClick.AddListener(NextScenario);

        if (CaseProgressManager.Instance != null)
            InvokeRepeating("CheckCaseResolution", 1f, 1f);
    }

    void CheckCaseResolution()
    {
        if (CaseProgressManager.Instance == null) return;
        if (TimerManager.Instance == null) return;

        if (CaseProgressManager.Instance.phase == CasePhase.Resolved &&
            endPanel != null && !endPanel.activeSelf)
        {
            float timeUsed = TimerManager.Instance.timeElapsed;
            ShowGameEnd(true, timeUsed, "Case solved! Well done, investigator.");

            if (TimerManager.Instance.isRunning)
                TimerManager.Instance.PauseTimer();
        }
    }

    public void ShowGameEnd(bool success, float timeUsed, string message = "")
    {
        if (endPanel == null) return;

        endPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        DisablePlayerControls();

        if (resultText != null)
        {
            resultText.text  = success ? "CASE SOLVED" : "CASE UNSOLVED";
            resultText.color = success ? successColor : failureColor;
        }

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeUsed / 60f);
            int seconds = Mathf.FloorToInt(timeUsed % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        if (messageText != null)
            messageText.text = message;

        // Show next-scenario button only if this isn't the last in the play order
        bool isLast = ScenarioManager.Instance != null && ScenarioManager.Instance.IsLastScenario;
        if (nextScenarioButton != null)
            nextScenarioButton.gameObject.SetActive(!isLast);

        // Survey reminder — different text for mid-study vs final scenario
        if (surveyText != null) surveyText.text = isLast ? surveyMessageFinal : surveyMessageMid;

        DataRecorder.Instance?.EndSession();

        Debug.Log($"[GameEnd] Success: {success}, Time: {timeUsed:F1}s");
    }

    public void HideGameEnd()
    {
        endPanel?.SetActive(false);
    }

    void NextScenario()
    {
        ScenarioManager.Instance?.NextScenario();
    }

    void DisablePlayerControls()
    {
        SimpleFPSController fps = FindObjectOfType<SimpleFPSController>();
        if (fps != null) fps.enabled = false;

        PlayerInteraction interaction = FindObjectOfType<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;

        DialogueInputUI dialogueUI = FindObjectOfType<DialogueInputUI>();
        if (dialogueUI != null) dialogueUI.Hide();
    }

    void OnDestroy()
    {
        CancelInvoke("CheckCaseResolution");
    }
}
