using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SessionData
{
    public string sessionId;
    public string playerId;
    public int scenarioIndex;
    public string persona;
    public string modality;
    public string startTime;
    public string endTime;
    public float totalDuration;
    public bool caseResolved;
    public string finalAnswer;
    public List<ConversationEntry> conversations = new List<ConversationEntry>();
    public List<EvidenceEntry> evidenceCollected = new List<EvidenceEntry>();
    public List<PhaseEntry> phaseTransitions = new List<PhaseEntry>();
}

[Serializable]
public class ConversationEntry
{
    public float timestamp;
    public string speaker; // "Player" or "Mentor"
    public string message;
    public string inputMethod; // "Text" or "Voice"
}

[Serializable]
public class EvidenceEntry
{
    public float timestamp;
    public string evidenceId;
    public string evidenceDescription;
}

[Serializable]
public class PhaseEntry
{
    public float timestamp;
    public string phaseName;
}

public class DataRecorder : MonoBehaviour
{
    public static DataRecorder Instance { get; private set; }

    [Header("Settings")]
    public string playerId = "P001"; // Set this for each participant
    public bool autoSaveOnGameEnd = true;
    public string saveDirectory = "ExperimentData";

    [Header("Current Session")]
    public SessionData currentSession;

    private float sessionStartTime;
    private string lastPhase = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartNewSession();
    }

    void Update()
    {
        // Track phase transitions
        if (CaseProgressManager.Instance != null)
        {
            string currentPhase = CaseProgressManager.Instance.phase.ToString();
            if (currentPhase != lastPhase)
            {
                RecordPhaseTransition(currentPhase);
                lastPhase = currentPhase;
            }
        }
    }

    /// <summary>
    /// Start a new recording session
    /// </summary>
    public void StartNewSession()
    {
        sessionStartTime = Time.time;

        currentSession = new SessionData
        {
            sessionId = GenerateSessionId(),
            playerId = this.playerId,
            startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        // Get scenario info
        if (ScenarioManager.Instance != null)
        {
            currentSession.scenarioIndex = ScenarioManager.Instance.scenarioIndex;
            currentSession.persona = ScenarioManager.Instance.persona.ToString();
            currentSession.modality = ScenarioManager.Instance.modality.ToString();
        }

        Debug.Log($"[DataRecorder] Started new session: {currentSession.sessionId}");
    }

    /// <summary>
    /// Record a conversation entry
    /// </summary>
    public void RecordConversation(string speaker, string message, string inputMethod = "Text")
    {
        if (currentSession == null) return;

        ConversationEntry entry = new ConversationEntry
        {
            timestamp = Time.time - sessionStartTime,
            speaker = speaker,
            message = message,
            inputMethod = inputMethod
        };

        currentSession.conversations.Add(entry);
        Debug.Log($"[DataRecorder] Recorded conversation: {speaker} ({inputMethod})");
    }

    /// <summary>
    /// Record evidence collection
    /// </summary>
    public void RecordEvidenceCollected(string evidenceId, string description)
    {
        if (currentSession == null) return;

        EvidenceEntry entry = new EvidenceEntry
        {
            timestamp = Time.time - sessionStartTime,
            evidenceId = evidenceId,
            evidenceDescription = description
        };

        currentSession.evidenceCollected.Add(entry);
        Debug.Log($"[DataRecorder] Recorded evidence: {evidenceId}");
    }

    /// <summary>
    /// Record case phase transition
    /// </summary>
    public void RecordPhaseTransition(string phaseName)
    {
        if (currentSession == null) return;

        PhaseEntry entry = new PhaseEntry
        {
            timestamp = Time.time - sessionStartTime,
            phaseName = phaseName
        };

        currentSession.phaseTransitions.Add(entry);
        Debug.Log($"[DataRecorder] Recorded phase transition: {phaseName}");
    }

    /// <summary>
    /// Record final answer
    /// </summary>
    public void RecordFinalAnswer(string answer, bool correct)
    {
        if (currentSession == null) return;

        currentSession.finalAnswer = answer;
        currentSession.caseResolved = correct;

        Debug.Log($"[DataRecorder] Recorded final answer - Correct: {correct}");
    }

    /// <summary>
    /// End current session and save data
    /// </summary>
    public void EndSession(bool saveData = true)
    {
        if (currentSession == null) return;

        currentSession.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSession.totalDuration = Time.time - sessionStartTime;

        // Get final case resolution status
        if (CaseProgressManager.Instance != null)
        {
            currentSession.caseResolved = CaseProgressManager.Instance.phase == CasePhase.Resolved;
        }

        Debug.Log($"[DataRecorder] Session ended - Duration: {currentSession.totalDuration:F1}s");

        if (saveData)
        {
            SaveSessionData();
        }
    }

    /// <summary>
    /// Save session data to JSON file
    /// </summary>
    public void SaveSessionData()
    {
        if (currentSession == null)
        {
            Debug.LogWarning("[DataRecorder] No session data to save");
            return;
        }

        try
        {
            // Create directory if it doesn't exist
            string dirPath = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // Generate filename with timestamp
            string filename = $"Session_{currentSession.sessionId}_{currentSession.playerId}_S{currentSession.scenarioIndex}.json";
            string filePath = Path.Combine(dirPath, filename);

            // Convert to JSON
            string json = JsonUtility.ToJson(currentSession, prettyPrint: true);

            // Write to file
            File.WriteAllText(filePath, json);

            Debug.Log($"[DataRecorder] Session data saved to: {filePath}");
            Debug.Log($"[DataRecorder] Total conversations: {currentSession.conversations.Count}");
            Debug.Log($"[DataRecorder] Total evidence collected: {currentSession.evidenceCollected.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataRecorder] Failed to save session data: {e.Message}");
        }
    }

    /// <summary>
    /// Export all session data to CSV (for easier analysis)
    /// </summary>
    public void ExportToCSV()
    {
        if (currentSession == null) return;

        try
        {
            string dirPath = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // Export conversations
            string convCsvPath = Path.Combine(dirPath, $"Conversations_{currentSession.sessionId}.csv");
            using (StreamWriter writer = new StreamWriter(convCsvPath))
            {
                writer.WriteLine("Timestamp,Speaker,InputMethod,Message");
                foreach (var conv in currentSession.conversations)
                {
                    string msg = conv.message.Replace("\"", "\"\""); // Escape quotes
                    writer.WriteLine($"{conv.timestamp:F2},{conv.speaker},{conv.inputMethod},\"{msg}\"");
                }
            }

            // Export evidence
            string evidCsvPath = Path.Combine(dirPath, $"Evidence_{currentSession.sessionId}.csv");
            using (StreamWriter writer = new StreamWriter(evidCsvPath))
            {
                writer.WriteLine("Timestamp,EvidenceID,Description");
                foreach (var evid in currentSession.evidenceCollected)
                {
                    string desc = evid.evidenceDescription.Replace("\"", "\"\"");
                    writer.WriteLine($"{evid.timestamp:F2},{evid.evidenceId},\"{desc}\"");
                }
            }

            Debug.Log($"[DataRecorder] CSV files exported to: {dirPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataRecorder] Failed to export CSV: {e.Message}");
        }
    }

    /// <summary>
    /// Generate unique session ID
    /// </summary>
    private string GenerateSessionId()
    {
        return DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    /// <summary>
    /// Get save directory path
    /// </summary>
    public string GetSaveDirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, saveDirectory);
    }

    void OnApplicationQuit()
    {
        // Auto-save on quit if enabled
        if (autoSaveOnGameEnd && currentSession != null)
        {
            EndSession(saveData: true);
        }
    }
}
