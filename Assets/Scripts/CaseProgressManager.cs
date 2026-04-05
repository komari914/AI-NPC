using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CasePhase
{
    Opening,        // 开场白播放中/刚结束
    Overview,       // 仅案件总览（受害者+3嫌疑人）
    Investigate,    // 常规调查（根据已拿线索引导）
    FocusOnClue,    // 发现关键线索后，聚焦讨论该线索（可选）
    FinalQuestion,  // 证据齐全后：导师要求玩家给出嫌疑人+理由
    Resolved        // 玩家答对，结案
}

public class CaseProgressManager : MonoBehaviour
{
    public static CaseProgressManager Instance { get; private set; }

    [Header("Phase")]
    public CasePhase phase = CasePhase.Opening;

    [Tooltip("玩家第一次跟 Mentor 说完话后，自动进入 Investigate。")]
    public bool hasHadFirstMentorTalk = false;

    [Header("Key Clue (optional)")]
    public string currentFocusClueId = "";  // 例如 "A5_WIFI_LOG"

    [Header("Final Answer Settings")]
    [Tooltip("收集到这些证据后，进入 FinalQuestion（导师要求玩家给出结论）。")]
    public string[] evidenceRequiredForFinal = new string[] { "A3", "P3", "J2", "J3" };

    [Tooltip("正确凶手名字（用于代码判定）。建议用 'Alex'。")]
    public string correctKillerName = "Alex";

    // 证据池：id -> description
    private readonly Dictionary<string, string> inspected = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void MarkOpeningFinished()
    {
        if (phase == CasePhase.Opening)
            phase = CasePhase.Investigate;
    }

    public void MarkFirstMentorTalkFinished()
    {
        hasHadFirstMentorTalk = true;
    }

    public void OnEvidenceInspected(string evidenceId, string description)
    {
        if (string.IsNullOrWhiteSpace(evidenceId)) return;

        inspected[evidenceId] = description ?? "";

        // 已结案则不再推进
        if (phase == CasePhase.Resolved || phase == CasePhase.FinalQuestion) return;

        // 发现关键线索时进入 FocusOnClue
        if (evidenceId == "A3")
        {
            currentFocusClueId = evidenceId;
            phase = CasePhase.FocusOnClue;
        }
        // FinalQuestion 不再由证据触发，改为由计时器触发
    }

    /// <summary>由计时器（倒计时警告）触发，进入最终阶段。</summary>
    public void TriggerFinalQuestion()
    {
        if (phase == CasePhase.Resolved) return;
        phase = CasePhase.FinalQuestion;
    }

    public bool HasClue(string evidenceId) => inspected.ContainsKey(evidenceId);

    public int InspectedCount() => inspected.Count;

    public bool IsReadyForFinal()
    {
        if (evidenceRequiredForFinal == null || evidenceRequiredForFinal.Length == 0)
            return false;

        return evidenceRequiredForFinal.All(id => !string.IsNullOrWhiteSpace(id) && inspected.ContainsKey(id));
    }

    public void MarkResolved()
    {
        phase = CasePhase.Resolved;
    }

    public string BuildEvidenceSummary(int maxItems = 6)
    {
        if (inspected.Count == 0) return "No evidence has been inspected yet.";

        var items = inspected.Take(maxItems)
            .Select(kv => $"- [{kv.Key}] {kv.Value}".Trim());

        return string.Join("\n", items);
    }

    public string BuildEvidenceSummaryAll()
    {
        return BuildEvidenceSummary(maxItems: 999);
    }
}
