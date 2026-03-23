using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainPanel;
    public GameObject instructionPanel;

    [Header("Scenario Buttons")]
    public Button scenario1Button;
    public Button scenario2Button;
    public Button scenario3Button;
    public Button scenario4Button;

    [Header("Other Buttons")]
    public Button instructionsButton;
    public Button backButton;
    public Button quitButton;

    [Header("Scenario Info Display (Optional)")]
    public TextMeshProUGUI scenarioInfoText;

    [Header("Instructions Text")]
    [TextArea(10, 20)]
    public string instructionsContent =
@"<b>Detective Investigation Game - Instructions</b>

<b>Objective:</b>
Investigate the murder of Daniel, the Project Lead, and identify the killer among three suspects.

<b>How to Play:</b>
1. Use WASD to move, Mouse to look around
2. Press E to inspect evidence items
3. Press F to talk to your mentor when nearby
4. Collect evidence and discuss findings with your mentor
5. Once you have enough evidence, present your conclusion

<b>Suspects:</b>
- Alex (Senior Systems Designer)
- Project Manager
- Junior Programmer

<b>Time Limit:</b>
You have 6 minutes to solve the case.

<b>Controls:</b>
• WASD - Move
• Mouse - Look
• E - Inspect Evidence
• F - Talk to Mentor
• ESC - Pause Menu

Good luck, investigator!";

    void Start()
    {
        ShowMainMenu();
        SetupButtons();
        UpdateScenarioInfoDisplay();
    }

    void SetupButtons()
    {
        if (scenario1Button != null)
            scenario1Button.onClick.AddListener(() => StartScenario(1));

        if (scenario2Button != null)
            scenario2Button.onClick.AddListener(() => StartScenario(2));

        if (scenario3Button != null)
            scenario3Button.onClick.AddListener(() => StartScenario(3));

        if (scenario4Button != null)
            scenario4Button.onClick.AddListener(() => StartScenario(4));

        if (instructionsButton != null)
            instructionsButton.onClick.AddListener(ShowInstructions);

        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void StartScenario(int index)
    {
        Debug.Log($"[MainMenu] Starting scenario {index}");

        // Get scenario configuration
        ScenarioConfig config = ScenarioManager.GetScenarioConfigByIndex(index);

        // Save to PlayerPrefs so SampleScene can load it
        PlayerPrefs.SetInt("ScenarioIndex", config.scenarioIndex);
        PlayerPrefs.SetInt("ScenarioPersona", (int)config.persona);
        PlayerPrefs.SetInt("ScenarioModality", (int)config.modality);
        PlayerPrefs.Save();

        Debug.Log($"[MainMenu] Saved scenario config: {config.scenarioName} ({config.description})");

        // Load game scene - the ScenarioManager in that scene will load the config
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    void ShowMainMenu()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    void ShowInstructions()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void UpdateScenarioInfoDisplay()
    {
        if (scenarioInfoText == null) return;

        scenarioInfoText.text =
@"<b>Select a Scenario:</b>

<b>Scenario 1:</b> Empathic Mentor + Text Chat
<b>Scenario 2:</b> Empathic Mentor + Voice Chat
<b>Scenario 3:</b> Task-Focused Mentor + Text Chat
<b>Scenario 4:</b> Task-Focused Mentor + Voice Chat";
    }
}
