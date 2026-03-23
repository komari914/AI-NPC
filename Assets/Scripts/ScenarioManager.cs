using UnityEngine;
using UnityEngine.SceneManagement;

public enum PersonaType { Empathic, TaskFocused }
public enum ModalityType { Text, Voice }

[System.Serializable]
public class ScenarioConfig
{
    public int scenarioIndex;           // 1-4
    public PersonaType persona;
    public ModalityType modality;
    public string scenarioName;
    public string description;

    public ScenarioConfig(int index, PersonaType p, ModalityType m)
    {
        scenarioIndex = index;
        persona = p;
        modality = m;
        scenarioName = $"Scenario {index}";
        description = $"{p} + {m}";
    }
}

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("Current Scenario")]
    public PersonaType persona = PersonaType.Empathic;
    public ModalityType modality = ModalityType.Text;
    public int scenarioIndex = 1; // 1-4

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "SampleScene";

    [Header("Display")]
    public bool showOverlay = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Load scenario config from PlayerPrefs if available
        LoadScenarioFromPrefs();
    }

    void Start()
    {
        // After all managers are initialized, check if we should notify them
        if (scenarioIndex > 0)
        {
            Debug.Log($"[ScenarioManager] Loaded Scenario {scenarioIndex}: {persona} + {modality}");
            NotifyScenarioLoaded();
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        int w = 320;
        int h = 90;
        GUI.Box(new Rect(10, 10, w, h), "Scenario Status");

        GUI.Label(new Rect(20, 35, w - 20, 20), $"Scenario: {scenarioIndex}");
        GUI.Label(new Rect(20, 55, w - 20, 20), $"Persona: {persona}");
        GUI.Label(new Rect(20, 75, w - 20, 20), $"Modality: {modality}");
    }

    /// <summary>
    /// Set and start a specific scenario
    /// </summary>
    public void StartScenario(ScenarioConfig config)
    {
        ApplyScenarioConfig(config);
        SaveScenarioToPrefs(config);

        Debug.Log($"[ScenarioManager] Starting {config.scenarioName}: {config.description}");

        // Always reload the game scene to reset all state (handles NextScenario from within game)
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Quick start with index (1-4), automatically assigns persona and modality
    /// </summary>
    public void StartScenarioByIndex(int index)
    {
        ScenarioConfig config = GetScenarioConfigByIndex(index);
        StartScenario(config);
    }

    /// <summary>
    /// Apply scenario configuration to current manager
    /// </summary>
    private void ApplyScenarioConfig(ScenarioConfig config)
    {
        scenarioIndex = config.scenarioIndex;
        persona = config.persona;
        modality = config.modality;
    }

    /// <summary>
    /// Get predefined scenario configuration by index
    /// Scenario 1: Empathic + Text
    /// Scenario 2: Empathic + Voice
    /// Scenario 3: TaskFocused + Text
    /// Scenario 4: TaskFocused + Voice
    /// </summary>
    public static ScenarioConfig GetScenarioConfigByIndex(int index)
    {
        switch (index)
        {
            case 1:
                return new ScenarioConfig(1, PersonaType.Empathic, ModalityType.Text);
            case 2:
                return new ScenarioConfig(2, PersonaType.Empathic, ModalityType.Voice);
            case 3:
                return new ScenarioConfig(3, PersonaType.TaskFocused, ModalityType.Text);
            case 4:
                return new ScenarioConfig(4, PersonaType.TaskFocused, ModalityType.Voice);
            default:
                Debug.LogWarning($"[ScenarioManager] Invalid scenario index: {index}, defaulting to 1");
                return new ScenarioConfig(1, PersonaType.Empathic, ModalityType.Text);
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[ScenarioManager] Returning to main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Restart current scenario
    /// </summary>
    public void RestartScenario()
    {
        Debug.Log("[ScenarioManager] Restarting current scenario");

        // Get current configuration and restart
        ScenarioConfig config = new ScenarioConfig(scenarioIndex, persona, modality);
        SaveScenarioToPrefs(config);

        // Reload the game scene
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Get current scenario configuration
    /// </summary>
    public ScenarioConfig GetCurrentScenario()
    {
        return new ScenarioConfig(scenarioIndex, persona, modality);
    }

    /// <summary>
    /// Save scenario configuration to PlayerPrefs
    /// </summary>
    private void SaveScenarioToPrefs(ScenarioConfig config)
    {
        PlayerPrefs.SetInt("ScenarioIndex", config.scenarioIndex);
        PlayerPrefs.SetInt("ScenarioPersona", (int)config.persona);
        PlayerPrefs.SetInt("ScenarioModality", (int)config.modality);
        PlayerPrefs.Save();
        Debug.Log($"[ScenarioManager] Saved scenario to PlayerPrefs: {config.scenarioIndex}");
    }

    /// <summary>
    /// Load scenario configuration from PlayerPrefs
    /// </summary>
    private void LoadScenarioFromPrefs()
    {
        if (PlayerPrefs.HasKey("ScenarioIndex"))
        {
            scenarioIndex = PlayerPrefs.GetInt("ScenarioIndex", 1);
            persona = (PersonaType)PlayerPrefs.GetInt("ScenarioPersona", 0);
            modality = (ModalityType)PlayerPrefs.GetInt("ScenarioModality", 0);
            Debug.Log($"[ScenarioManager] Loaded from PlayerPrefs: Scenario {scenarioIndex} ({persona} + {modality})");
        }
    }

    /// <summary>
    /// Notify other systems that scenario has been loaded
    /// </summary>
    private void NotifyScenarioLoaded()
    {
        // This can be extended to notify other systems
        // For now, just log
        Debug.Log($"[ScenarioManager] Scenario {scenarioIndex} is active");
    }

    /// <summary>
    /// Clear saved scenario data (useful for testing)
    /// </summary>
    public void ClearScenarioPrefs()
    {
        PlayerPrefs.DeleteKey("ScenarioIndex");
        PlayerPrefs.DeleteKey("ScenarioPersona");
        PlayerPrefs.DeleteKey("ScenarioModality");
        PlayerPrefs.Save();
        Debug.Log("[ScenarioManager] Cleared scenario PlayerPrefs");
    }
}