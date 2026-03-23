using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;

    [Header("Color Settings")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;

    [Header("Warning Thresholds")]
    public float warningTime = 120f;  // 2 minutes
    public float criticalTime = 60f;  // 1 minute

    [Header("Animation")]
    public bool pulseWhenLow = true;
    public float pulseSpeed = 2f;

    private TimerManager timerManager;
    private bool isWarning = false;
    private bool isCritical = false;

    void Start()
    {
        timerManager = TimerManager.Instance;

        if (timerManager == null)
        {
            Debug.LogWarning("[TimerUI] TimerManager instance not found!");
            return;
        }

        // Subscribe to timer events
        if (timerManager.onTimerWarning != null)
        {
            timerManager.onTimerWarning.AddListener(OnWarning);
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (timerManager == null) return;

        UpdateDisplay();
        UpdateColor();

        if (pulseWhenLow && isCritical)
        {
            PulseEffect();
        }
    }

    void UpdateDisplay()
    {
        if (timerText == null) return;

        string timeString = timerManager.GetFormattedTime();
        timerText.text = timeString;
    }

    void UpdateColor()
    {
        if (timerText == null) return;

        float remaining = timerManager.timeRemaining;

        if (remaining <= criticalTime && !isCritical)
        {
            isCritical = true;
            timerText.color = criticalColor;
        }
        else if (remaining <= warningTime && remaining > criticalTime && !isWarning)
        {
            isWarning = true;
            timerText.color = warningColor;
        }
        else if (remaining > warningTime)
        {
            timerText.color = normalColor;
            isWarning = false;
            isCritical = false;
        }
    }

    void PulseEffect()
    {
        if (timerText == null) return;

        float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float scale = 1f + (pulse * 0.1f);
        timerText.transform.localScale = Vector3.one * scale;
    }

    void OnWarning()
    {
        Debug.Log("[TimerUI] Warning triggered!");
        // You can add additional visual/audio effects here
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (timerManager != null && timerManager.onTimerWarning != null)
        {
            timerManager.onTimerWarning.RemoveListener(OnWarning);
        }
    }
}
