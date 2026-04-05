using System;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Duration in seconds (360 = 6 minutes)")]
    public float totalDuration = 360f; // 6 minutes

    [Tooltip("Auto-start timer when scene loads")]
    public bool autoStart = true;

    [Header("Timer State")]
    public bool isRunning = false;
    public float timeRemaining;
    public float timeElapsed = 0f;

    [Header("Events")]
    public UnityEvent onTimerStart;
    public UnityEvent onTimerComplete;
    public UnityEvent onTimerWarning; // Triggered at 1 minute remaining

    [Header("Warnings")]
    public float warningTime = 60f; // Show warning at 1 minute
    private bool warningTriggered = false;

    [Header("Auto End Game")]
    public bool autoEndGameOnComplete = true;
    public GameEndUI gameEndUI;

    [Header("Final Question Trigger")]
    [Tooltip("Assign the MentorNPC in scene — called when timer warning fires")]
    public MentorNPC mentorNPC;

    [Header("Testing (Debug Only)")]
    [Tooltip("Press T key to set timer to 5 seconds remaining (for testing)")]
    public bool enableTestShortcut = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timeRemaining = totalDuration;
    }

    void Start()
    {
        if (autoStart)
        {
            StartTimer();
        }
    }

    void Update()
    {
        // Testing shortcut: Press T to set timer to 5 seconds
        if (enableTestShortcut && UnityEngine.InputSystem.Keyboard.current != null &&
            (DialogueInputUI.Instance == null || !DialogueInputUI.Instance.IsOpen))
        {
            if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                SetTimeRemaining(5f);
                Debug.Log("[Timer] TEST MODE: Set timer to 5 seconds remaining");
            }
        }

        if (!isRunning) return;

        // Update timer
        timeElapsed += Time.deltaTime;
        timeRemaining = Mathf.Max(0f, totalDuration - timeElapsed);

        // Check for warning time
        if (!warningTriggered && timeRemaining <= warningTime && timeRemaining > 0f)
        {
            warningTriggered = true;
            onTimerWarning?.Invoke();
            Debug.Log($"[Timer] Warning: {warningTime} seconds remaining!");

            // Trigger FinalQuestion phase and mentor prompt
            CaseProgressManager.Instance?.TriggerFinalQuestion();
            if (mentorNPC == null) mentorNPC = UnityEngine.Object.FindFirstObjectByType<MentorNPC>();
            mentorNPC?.StartFinalPhasePrompt();
        }

        // Check for completion
        if (timeRemaining <= 0f)
        {
            CompleteTimer();
        }
    }

    /// <summary>
    /// Start or resume the timer
    /// </summary>
    public void StartTimer()
    {
        if (isRunning) return;

        isRunning = true;
        onTimerStart?.Invoke();
        Debug.Log($"[Timer] Started: {totalDuration} seconds ({totalDuration / 60f:F1} minutes)");
    }

    /// <summary>
    /// Pause the timer
    /// </summary>
    public void PauseTimer()
    {
        if (!isRunning) return;

        isRunning = false;
        Debug.Log($"[Timer] Paused at {timeRemaining:F1} seconds remaining");
    }

    /// <summary>
    /// Resume the timer
    /// </summary>
    public void ResumeTimer()
    {
        if (isRunning) return;

        isRunning = true;
        Debug.Log($"[Timer] Resumed with {timeRemaining:F1} seconds remaining");
    }

    /// <summary>
    /// Stop and reset the timer
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
        timeElapsed = 0f;
        timeRemaining = totalDuration;
        warningTriggered = false;
        Debug.Log("[Timer] Stopped and reset");
    }

    /// <summary>
    /// Called when timer reaches zero
    /// </summary>
    private void CompleteTimer()
    {
        if (!isRunning) return;

        isRunning = false;
        timeRemaining = 0f;
        onTimerComplete?.Invoke();

        Debug.Log("[Timer] Time's up!");

        if (autoEndGameOnComplete)
        {
            EndGameDueToTimeout();
        }
    }

    /// <summary>
    /// End game when time runs out
    /// </summary>
    private void EndGameDueToTimeout()
    {
        Debug.Log("[Timer] Ending game due to timeout");

        // Check if case is already resolved
        bool caseResolved = CaseProgressManager.Instance != null &&
                           CaseProgressManager.Instance.phase == CasePhase.Resolved;

        if (gameEndUI != null)
        {
            if (caseResolved)
            {
                gameEndUI.ShowGameEnd(true, timeElapsed, "Case solved!");
            }
            else
            {
                gameEndUI.ShowGameEnd(false, timeElapsed, "Time's up! Case not solved.");
            }
        }
        else
        {
            Debug.LogWarning("[Timer] GameEndUI not assigned, cannot show end screen");
        }
    }

    /// <summary>
    /// Get formatted time string (MM:SS)
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Get formatted elapsed time string (MM:SS)
    /// </summary>
    public string GetFormattedElapsedTime()
    {
        int minutes = Mathf.FloorToInt(timeElapsed / 60f);
        int seconds = Mathf.FloorToInt(timeElapsed % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Get progress as 0-1 value
    /// </summary>
    public float GetProgress()
    {
        return Mathf.Clamp01(timeElapsed / totalDuration);
    }

    /// <summary>
    /// Add extra time to the timer
    /// </summary>
    public void AddTime(float seconds)
    {
        timeElapsed = Mathf.Max(0f, timeElapsed - seconds);
        timeRemaining = Mathf.Max(0f, totalDuration - timeElapsed);
        Debug.Log($"[Timer] Added {seconds} seconds. New remaining: {timeRemaining:F1}s");
    }

    /// <summary>
    /// Set timer to specific remaining time (for testing)
    /// </summary>
    public void SetTimeRemaining(float seconds)
    {
        timeElapsed = totalDuration - seconds;
        timeRemaining = Mathf.Max(0f, seconds);
        Debug.Log($"[Timer] Set remaining time to: {timeRemaining:F1}s");
    }
}
