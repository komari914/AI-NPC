using UnityEngine;
using UnityEngine.SceneManagement;

public enum PersonaType { Empathic, TaskFocused }
public enum ModalityType { Text, Voice }

[System.Serializable]
public class ScenarioConfig
{
    public int          scenarioIndex;
    public PersonaType  persona;
    public ModalityType modality;
    public string       scenarioName;
    public string       description;

    public ScenarioConfig(int index, PersonaType p, ModalityType m)
    {
        scenarioIndex = index;
        persona       = p;
        modality      = m;
        scenarioName  = $"Scenario {index}";
        description   = $"{p} + {m}";
    }
}

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("Current Scenario (read-only at runtime)")]
    public PersonaType  persona       = PersonaType.TaskFocused;
    public ModalityType modality      = ModalityType.Text;
    public int          scenarioIndex = 3;

    [Header("Scene")]
    public string gameSceneName = "SampleScene";

    [Header("Debug Overlay")]
    public bool showOverlay = false;

    // Fixed play order for all participants:
    // 1st: Scenario 3 (TaskFocused + Text)
    // 2nd: Scenario 2 (Empathic    + Voice)
    // 3rd: Scenario 1 (Empathic    + Text)
    // 4th: Scenario 4 (TaskFocused + Voice)
    private static readonly int[] PlayOrder = { 3, 2, 1, 4 };

    private const string KeyPosition = "ScenarioPosition"; // 0–3

    public int  CurrentOrderPosition { get; private set; }
    public bool IsLastScenario       => CurrentOrderPosition >= PlayOrder.Length - 1;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Initialise();
    }

    void Start()
    {
        Debug.Log($"[ScenarioManager] Step {CurrentOrderPosition + 1}/4 — " +
                  $"Scenario {scenarioIndex} ({persona} + {modality})");
    }

    void OnGUI()
    {
        if (!showOverlay) return;
        GUI.Box  (new Rect(10, 10, 320, 90), "Scenario Status");
        GUI.Label(new Rect(20, 35, 300, 20), $"Step: {CurrentOrderPosition + 1} / 4");
        GUI.Label(new Rect(20, 55, 300, 20), $"Scenario {scenarioIndex}: {persona}");
        GUI.Label(new Rect(20, 75, 300, 20), $"Modality: {modality}");
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    void Initialise()
    {
        CurrentOrderPosition = Mathf.Clamp(PlayerPrefs.GetInt(KeyPosition, 0), 0, PlayOrder.Length - 1);
        ApplyConfig(GetScenarioConfigByIndex(PlayOrder[CurrentOrderPosition]));
    }

    void ApplyConfig(ScenarioConfig config)
    {
        scenarioIndex = config.scenarioIndex;
        persona       = config.persona;
        modality      = config.modality;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Load the next scenario in the fixed play order.</summary>
    public void NextScenario()
    {
        if (IsLastScenario)
        {
            Debug.Log("[ScenarioManager] All 4 scenarios completed.");
            return;
        }

        PlayerPrefs.SetInt(KeyPosition, CurrentOrderPosition + 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Resets progress back to step 1. Call this when starting a new participant
    /// session (e.g. from a hidden reset button or the Unity Editor).
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.SetInt(KeyPosition, 0);
        PlayerPrefs.Save();
        Debug.Log("[ScenarioManager] Progress reset — starting from step 1.");

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public ScenarioConfig GetCurrentScenario() =>
        new ScenarioConfig(scenarioIndex, persona, modality);

    public static ScenarioConfig GetScenarioConfigByIndex(int index) => index switch
    {
        1 => new ScenarioConfig(1, PersonaType.Empathic,    ModalityType.Text),
        2 => new ScenarioConfig(2, PersonaType.Empathic,    ModalityType.Voice),
        3 => new ScenarioConfig(3, PersonaType.TaskFocused, ModalityType.Text),
        4 => new ScenarioConfig(4, PersonaType.TaskFocused, ModalityType.Voice),
        _ => new ScenarioConfig(1, PersonaType.Empathic,    ModalityType.Text),
    };
}
