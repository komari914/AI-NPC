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

    [Header("Editor Debug")]
    [Tooltip("Enable to bypass the fixed play order and jump straight to a specific scenario.")]
    public bool debugOverride = false;
    [Tooltip("1 = TaskFocused+Text, 2 = Empathic+Voice, 3 = Empathic+Text, 4 = TaskFocused+Voice")]
    [Range(1, 4)]
    public int  debugScenarioIndex = 1;

    // Randomised per participant — generated fresh on every new launch
    private int[] _playOrder = { 1, 2, 3, 4 };

    private const string KeyPosition  = "ScenarioPosition"; // 0–3
    private const string KeyPlayerID  = "PlayerID";
    private const string KeyPlayOrder = "PlayOrder";        // stored as "2,4,1,3"

    // True only when NextScenario() reloads the scene mid-session.
    // Clears automatically on fresh Play Mode entry (editor) or fresh browser load (WebGL).
    private static bool _isMidSessionReload = false;

    public int    CurrentOrderPosition { get; private set; }
    public bool   IsLastScenario       => CurrentOrderPosition >= _playOrder.Length - 1;
    public string PlayerID             { get; private set; } = "P001";

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
        GUI.Box  (new Rect(10, 10, 320, 70), "Session Info");
        GUI.Label(new Rect(20, 35, 300, 20), $"Player: {PlayerID}");
        GUI.Label(new Rect(20, 55, 300, 20), $"Scenario: {scenarioIndex}   |   Modality: {modality}");
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    void Initialise()
    {
        if (_isMidSessionReload)
        {
            // Scene was reloaded by NextScenario() — keep the same ID and order
            _isMidSessionReload      = false;
            PlayerID                 = PlayerPrefs.GetString(KeyPlayerID, "P001");
            _playOrder               = LoadPlayOrder();
            CurrentOrderPosition     = Mathf.Clamp(PlayerPrefs.GetInt(KeyPosition, 0), 0, _playOrder.Length - 1);
        }
        else
        {
            // Fresh launch (new Play Mode session, new browser load, or after ResetProgress)
            GenerateNewPlayerID();
            GenerateNewPlayOrder();
            CurrentOrderPosition = 0;
            PlayerPrefs.SetInt(KeyPosition, 0);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        if (debugOverride)
        {
            CurrentOrderPosition = 0;
            ApplyConfig(GetScenarioConfigByIndex(debugScenarioIndex));
            Debug.Log($"[ScenarioManager] DEBUG OVERRIDE — Scenario {debugScenarioIndex} ({persona} + {modality})");
            return;
        }
#endif
        ApplyConfig(GetScenarioConfigByIndex(_playOrder[CurrentOrderPosition]));
    }

    void GenerateNewPlayerID()
    {
        // Timestamp-based ID — unique across different devices/browsers in WebGL
        PlayerID = "P" + System.DateTime.Now.ToString("MMddHHmmss");
        PlayerPrefs.SetString(KeyPlayerID, PlayerID);
        PlayerPrefs.Save();
        Debug.Log($"[ScenarioManager] New Player ID assigned: {PlayerID}");
    }

    void GenerateNewPlayOrder()
    {
        // Fisher-Yates shuffle of [1, 2, 3, 4]
        _playOrder = new int[] { 1, 2, 3, 4 };
        for (int i = _playOrder.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_playOrder[i], _playOrder[j]) = (_playOrder[j], _playOrder[i]);
        }
        PlayerPrefs.SetString(KeyPlayOrder, string.Join(",", _playOrder));
        PlayerPrefs.Save();
        Debug.Log($"[ScenarioManager] New play order: {string.Join(", ", _playOrder)}");
    }

    int[] LoadPlayOrder()
    {
        string saved = PlayerPrefs.GetString(KeyPlayOrder, "1,2,3,4");
        string[] parts = saved.Split(',');
        int[] order = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            int.TryParse(parts[i], out order[i]);
        return order;
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

        _isMidSessionReload = true; // tell Initialise() this is a scene reload, not a fresh start
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
        _isMidSessionReload = false; // ensure Initialise() treats next load as a fresh start
        GenerateNewPlayerID();
        GenerateNewPlayOrder();
        PlayerPrefs.SetInt(KeyPosition, 0);
        PlayerPrefs.Save();
        Debug.Log($"[ScenarioManager] Progress reset — new participant {PlayerID}, order: {string.Join(", ", _playOrder)}");

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
