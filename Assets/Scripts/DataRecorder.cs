using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SessionData
{
    public string sessionId;
    public string playerId;
    public int    scenarioIndex;
    public string persona;
    public string modality;
    public string startTime;
    public string endTime;
    public float  totalDuration;
    public bool   caseResolved;
    public string finalAnswer;
    public List<ConversationEntry> conversations     = new List<ConversationEntry>();
    public List<EvidenceEntry>     evidenceCollected = new List<EvidenceEntry>();
    public List<PhaseEntry>        phaseTransitions  = new List<PhaseEntry>();
}

[Serializable]
public class ConversationEntry
{
    public float  timestamp;
    public string speaker;      // "Player" or "Mentor"
    public string message;
    public string inputMethod;  // "Text" or "Voice"
}

[Serializable]
public class EvidenceEntry
{
    public float  timestamp;
    public string evidenceId;
    public string evidenceDescription;
}

[Serializable]
public class PhaseEntry
{
    public float  timestamp;
    public string phaseName;
}

public class DataRecorder : MonoBehaviour
{
    public static DataRecorder Instance { get; private set; }

    [Header("Participant")]
    [Tooltip("Set a unique ID for each participant before the session")]
    public string playerId = "P001";

    [Header("Google Sheets")]
    [Tooltip("Paste your Google Apps Script Web App URL here")]
    public string googleScriptUrl = "";

    [Header("Current Session")]
    public SessionData currentSession;

    private float  sessionStartTime;
    private string lastPhase = "";

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartNewSession();
    }

    void Update()
    {
        if (CaseProgressManager.Instance == null) return;
        string currentPhase = CaseProgressManager.Instance.phase.ToString();
        if (currentPhase != lastPhase)
        {
            RecordPhaseTransition(currentPhase);
            lastPhase = currentPhase;
        }
    }

    void OnApplicationQuit()
    {
        EndSession();
    }

    // ─── Session control ──────────────────────────────────────────────────────

    public void StartNewSession()
    {
        sessionStartTime = Time.time;

        currentSession = new SessionData
        {
            sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            playerId  = this.playerId,
            startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        if (ScenarioManager.Instance != null)
        {
            currentSession.scenarioIndex = ScenarioManager.Instance.scenarioIndex;
            currentSession.persona       = ScenarioManager.Instance.persona.ToString();
            currentSession.modality      = ScenarioManager.Instance.modality.ToString();
        }

        Debug.Log($"[DataRecorder] Session started: {currentSession.sessionId}");
    }

    public void EndSession()
    {
        if (currentSession == null) return;

        currentSession.endTime       = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSession.totalDuration = Time.time - sessionStartTime;

        if (CaseProgressManager.Instance != null)
            currentSession.caseResolved = CaseProgressManager.Instance.phase == CasePhase.Resolved;

        StartCoroutine(UploadToGoogleSheets());
    }

    // ─── Recording ────────────────────────────────────────────────────────────

    public void RecordConversation(string speaker, string message, string inputMethod = "Text")
    {
        if (currentSession == null) return;
        currentSession.conversations.Add(new ConversationEntry
        {
            timestamp   = Time.time - sessionStartTime,
            speaker     = speaker,
            message     = message,
            inputMethod = inputMethod
        });
    }

    public void RecordEvidenceCollected(string evidenceId, string description)
    {
        if (currentSession == null) return;
        currentSession.evidenceCollected.Add(new EvidenceEntry
        {
            timestamp            = Time.time - sessionStartTime,
            evidenceId           = evidenceId,
            evidenceDescription  = description
        });
    }

    public void RecordFinalAnswer(string answer, bool correct)
    {
        if (currentSession == null) return;
        currentSession.finalAnswer  = answer;
        currentSession.caseResolved = correct;
    }

    public void RecordPhaseTransition(string phaseName)
    {
        if (currentSession == null) return;
        currentSession.phaseTransitions.Add(new PhaseEntry
        {
            timestamp = Time.time - sessionStartTime,
            phaseName = phaseName
        });
    }

    // ─── Upload ───────────────────────────────────────────────────────────────

    IEnumerator UploadToGoogleSheets()
    {
        if (string.IsNullOrWhiteSpace(googleScriptUrl))
        {
            Debug.LogWarning("[DataRecorder] Google Script URL not set — data not uploaded.");
            yield break;
        }

        string json    = JsonUtility.ToJson(currentSession);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        Debug.Log($"[DataRecorder] Uploading session {currentSession.sessionId} to Google Sheets...");

        using (UnityWebRequest request = new UnityWebRequest(googleScriptUrl, "POST"))
        {
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"[DataRecorder] Upload success: {request.downloadHandler.text}");
            else
                Debug.LogError($"[DataRecorder] Upload failed: {request.error}");
        }
    }
}
