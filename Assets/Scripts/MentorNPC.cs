using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MentorNPC : MonoBehaviour
{
    [Header("Interaction")]
    public float talkDistance = 3.0f;
    public Transform playerCamera;
    public Transform playerRoot;

    [Header("References")]
    public ScenarioManager scenarioManager;
    public SubtitleUI subtitleUI;
    public OpenAIClient openAI;

    [Header("Dialogue UI (optional, for player input)")]
    public DialogueInputUI dialogueUI;

    [Header("Voice Features (for Voice modality)")]
    public TTSManager ttsManager;
    public VoiceInputManager voiceInputManager;

    [Header("Animation")]
    public Animator npcAnimator;
    [Tooltip("Bool parameter name in the Animator Controller (true = talking, false = idle)")]
    public string talkingBoolParam = "IsTalking";

    [Header("Progress (Case State)")]
    public CaseProgressManager progress;

    [Header("Opening (Non-AI)")]
    public bool playOpeningOnStart = true;

    [TextArea(2, 6)]
    public string[] openingLines =
    {
        "Mentor: Listen carefully. Daniel was found dead in the office last night.",
        "Mentor: We don’t make assumptions. We follow facts and timelines.",
        "Mentor: Inspect anything that looks relevant, then come back and talk to me."
    };

    public float openingLineDelay = 2.6f;

    [Header("System Prompts (paste from Scenario Sample)")]
    [TextArea(8, 30)]
    public string empathicSystemPrompt =
@"You are an experienced detective and a warm, supportive mentor guiding a junior investigator through a murder case.

Persona:
- You genuinely care about the player's wellbeing. Naturally check in on them (e.g. 'How are you holding up with all this?', 'Take your time—this is a lot to process.').
- Use encouraging, affirming language. Acknowledge good observations (e.g. 'Good thinking.', 'That's a sharp eye—well spotted.').
- When the player is stuck or mistaken, respond with patient guidance rather than criticism.
- Keep your tone warm and human throughout.

Response style:
- 2–4 short bullet points or 2–3 sentences maximum.
- Encouraging but still professional. This is a serious case.";

    [TextArea(8, 30)]
    public string taskFocusedSystemPrompt =
@"You are an experienced detective and a direct, result-oriented team leader guiding a junior investigator through a murder case.

Persona:
- You are focused entirely on facts, evidence, and closing the case efficiently. Do not ask about the player's feelings or comfort.
- Give clear, direct instructions. No small talk or emotional support.
- If the player is wrong, correct them plainly and move on (e.g. 'That doesn't hold up. Check the timeline again.').
- Only acknowledge correct reasoning when the player's logic is concretely sound—no general encouragement.

Response style:
- 2–4 short bullet points or 2–3 sentences maximum.
- Strictly professional. Stick to evidence and logic only.";

    [Header("Base Context (optional but recommended)")]
    [TextArea(4, 12)]
    public string baseContext =
@"Context: A fictional office homicide case in a mid-sized software company. Non-graphic. No gore.

Victim:
- Daniel, Project Lead of Project Aurora.
- Cause of death: blunt force trauma. Weapon: a metal 'Project Excellence' trophy found wiped clean on his desk.
- Time of death: between 10:00 PM and 11:00 PM (medical examiner's estimate).
- Daniel stayed late to prepare materials for an internal review.

Known Suspects:
1. Alex (Senior Systems Designer) — led the early architecture phase. Daniel used Alex's core design ideas without attribution and blocked Alex's promotion.
2. Project Manager — clashed with Daniel over budget authority and timeline blame.
3. Junior Programmer — publicly belittled by Daniel; recently received a negative mid-cycle review threatening their probation.

Evidence summary (all items):
- A1: Alex's annotated design draft. Notes like 'this part was taken' confirm a credit dispute and long-term grievance.
- A2: Alex's taxi receipt (9:47 PM departure). Missing the platform's Late Night Fee surcharge — compare with P2 to expose it as fabricated.
- A3 [KEY]: Wi-Fi log. Alex's registered laptop reconnected to the office network at 10:23 PM, inside the death window. Directly contradicts A2.
- P1: Meeting notes documenting a serious dispute between Daniel and the PM over budget.
- P2: PM's taxi receipt (9:14 PM departure). Includes an itemised Late Night Fee — the standard surcharge for all rides after 9:00 PM. Exposes A2 as fake.
- P3 [KEY]: Calendar confirmation + video call log. PM was in a verified remote meeting 10:02 PM–11:17 PM. Eliminates PM.
- J1: Overtime log. Junior Programmer was clocked in past 11:00 PM — in the building during the death window.
- J2 [KEY]: Version control commit log. JP submitted code at 10:08, 10:37, and 10:59 PM from the developer floor.
- J3 [KEY]: Designer–JP chat log. Each commit was made in direct response to real-time requirement changes. JP could not have committed murder and returned to active coding within these intervals. Eliminates JP.

You are the player's mentor / team leader. Guide reasoning from evidence. Do not reveal the killer directly.
";

    [Header("Fallback Question (used if no input UI is assigned)")]
    [TextArea(2, 6)]
    public string fallbackPlayerQuestion =
        "Can you give me an overview of the case and the suspects?";

    [Header("Conversation History")]
    [Tooltip("Max turns kept in memory (each turn = 1 user + 1 assistant message)")]
    public int maxHistoryTurns = 6;

    private bool isBusy = false;
    private bool openingFinished = false;
    private string latestPlayerInput = "";
    private readonly List<ChatMessage> conversationHistory = new();

    void Start()
    {
        if (progress == null) progress = CaseProgressManager.Instance;

        // Auto-find player camera if not assigned in Inspector
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                playerCamera = cam.transform;
                Debug.Log("[MentorNPC] Auto-found Main Camera as playerCamera.");
            }
        }

        if (playOpeningOnStart && subtitleUI != null)
            StartCoroutine(PlayOpeningRoutine());
        else
        {
            openingFinished = true;
            progress?.MarkOpeningFinished();
        }
    }

    IEnumerator PlayOpeningRoutine()
    {
        openingFinished = false;

        foreach (var line in openingLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                // Use TTS in Voice modality
                if (IsVoiceModality() && ttsManager != null)
                {
                    subtitleUI.ShowPersistent(line);
                    SetTalking(true);

                    bool ttsComplete = false;
                    ttsManager.Speak(line,
                        onComplete: () => ttsComplete = true,
                        onError: (err) => { Debug.LogError($"[MentorNPC] TTS Error: {err}"); ttsComplete = true; }
                    );

                    yield return new WaitUntil(() => ttsComplete);

                    SetTalking(false);
                    subtitleUI.Clear();
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    // Text mode: estimate read time from line length
                    SetTalking(true);
                    subtitleUI.Show(line);
                    float readTime = Mathf.Max(2f, line.Length * 0.05f);
                    yield return new WaitForSeconds(readTime);
                    SetTalking(false);
                    yield return new WaitForSeconds(openingLineDelay - readTime > 0
                        ? openingLineDelay - readTime : 0.2f);
                }
            }
        }

        openingFinished = true;
        progress?.MarkOpeningFinished();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        if (!IsPlayerCloseEnough()) return;

        if (!openingFinished)
        {
            subtitleUI?.Show("Mentor: Finish the briefing first.");
            return;
        }

        // In Voice modality, F key is not used (Voice input uses V key in VoiceInputManager)
        if (IsVoiceModality())
        {
            subtitleUI?.Show("Mentor: Hold V to speak to me.");
            return;
        }

        // Text modality: show dialogue input UI
        if (dialogueUI != null)
        {
            dialogueUI.Show();
        }
        else
        {
            TalkWithPlayerInput(fallbackPlayerQuestion);
        }
    }

    public void TalkWithPlayerInput(string playerText)
    {
        if (subtitleUI == null)
        {
            Debug.LogWarning("[MentorNPC] SubtitleUI not assigned.");
            return;
        }

        if (progress != null && progress.phase == CasePhase.Resolved)
        {
            subtitleUI.Show("Mentor: The case is closed. Thanks for your work.");
            return;
        }

        if (openAI == null)
        {
            subtitleUI.Show("Mentor Error: OpenAIClient not assigned.");
            return;
        }

        if (!openingFinished)
        {
            subtitleUI.Show("Mentor: Finish the briefing first.");
            return;
        }

        if (!IsPlayerCloseEnough())
        {
            subtitleUI.Show("Mentor: Come closer if you want to talk.");
            return;
        }

        if (isBusy)
        {
            subtitleUI.Show("Mentor: (thinking...)");
            return;
        }

        latestPlayerInput = (playerText ?? "").Trim();
        Debug.Log($"[CHAT] Player: {latestPlayerInput}");

        if (string.IsNullOrWhiteSpace(latestPlayerInput))
        {
            subtitleUI.Show("Mentor: Say something, then we'll proceed.");
            return;
        }

        // Record player input
        if (DataRecorder.Instance != null)
        {
            string inputMethod = IsVoiceModality() ? "Voice" : "Text";
            DataRecorder.Instance.RecordConversation("Player", latestPlayerInput, inputMethod);
        }

        // Final phase: use AI to evaluate player's conclusion semantically.
        if (progress != null && progress.phase == CasePhase.FinalQuestion)
        {
            if (LooksLikeConclusion(latestPlayerInput))
            {
                subtitleUI.Show("Mentor: (evaluating your reasoning...)");
                isBusy = true;
                StartCoroutine(EvaluateConclusionWithAI(latestPlayerInput));
                return;
            }
            // Not a conclusion yet → let AI guide them with questions.
        }

        string systemPrompt = GetSystemPromptByPersona();
        if (string.IsNullOrWhiteSpace(systemPrompt) || systemPrompt.StartsWith("(PASTE"))
        {
            subtitleUI.Show("Mentor Error: Please paste the persona system prompt into MentorNPC.");
            return;
        }

        string userMessage = BuildUserMessage(latestPlayerInput);

        subtitleUI.Show("Mentor: (thinking...)");
        isBusy = true;

        StartCoroutine(openAI.SendWithHistory(
            systemPrompt,
            GetTrimmedHistory(),
            userMessage,
            onResult: (text) =>
            {
                isBusy = false;

                string cleaned = (text ?? "").Trim();
                Debug.Log($"[CHAT] Mentor: {cleaned}");

                // Append both turns to history
                conversationHistory.Add(new ChatMessage("user",      userMessage));
                conversationHistory.Add(new ChatMessage("assistant", cleaned));
                TrimHistory();

                // Record mentor response
                if (DataRecorder.Instance != null)
                {
                    string inputMethod = IsVoiceModality() ? "Voice" : "Text";
                    DataRecorder.Instance.RecordConversation("Mentor", cleaned, inputMethod);
                }

                // In Voice modality: show persistent subtitle and wait for TTS
                if (IsVoiceModality() && ttsManager != null)
                {
                    SetTalking(true);
                    subtitleUI.ShowPersistent("Mentor: " + cleaned);

                    ttsManager.Speak(cleaned,
                        onComplete: () =>
                        {
                            Debug.Log("[MentorNPC] TTS completed");
                            SetTalking(false);
                            if (subtitleUI != null)
                                StartCoroutine(ClearSubtitleAfterDelay(1.0f));
                        },
                        onError: (err) =>
                        {
                            Debug.LogError($"[MentorNPC] TTS Error: {err}");
                            SetTalking(false);
                            subtitleUI?.Clear();
                        }
                    );
                }
                else
                {
                    SetTalking(true);
                    subtitleUI.Show("Mentor: " + cleaned);
                    // Estimate talking duration from reply length (~50ms per char, min 3s)
                    StartCoroutine(StopTalkingAfterDelay(Mathf.Max(3f, cleaned.Length * 0.05f)));
                }

                // First successful exchange advances Overview → Investigate
                if (progress != null && !progress.hasHadFirstMentorTalk)
                    progress.MarkFirstMentorTalkFinished();
            },
            onError: (err) =>
            {
                isBusy = false;
                subtitleUI.Show("Mentor Error: " + err);
            }
        ));
    }

    /// <summary>Returns true if the player is within talk distance. Falls back to true if no player ref is set.</summary>
    public bool IsPlayerCloseEnough()
    {
        if (playerRoot != null)
            return Vector3.Distance(transform.position, playerRoot.position) <= talkDistance;

        if (playerCamera != null)
            return Vector3.Distance(transform.position, playerCamera.position) <= talkDistance;

        // No references assigned — assume close enough so voice can still work
        Debug.LogWarning("[MentorNPC] playerRoot and playerCamera are both null. Assign them in the Inspector.");
        return true;
    }

    bool IsVoiceModality()
    {
        if (scenarioManager == null) return false;
        return scenarioManager.modality == ModalityType.Voice;
    }

    List<ChatMessage> GetTrimmedHistory()
    {
        // Each turn = 2 messages (user + assistant); keep last N turns
        int keep = maxHistoryTurns * 2;
        if (conversationHistory.Count <= keep) return conversationHistory;
        return conversationHistory.GetRange(conversationHistory.Count - keep, keep);
    }

    void TrimHistory()
    {
        int keep = maxHistoryTurns * 2;
        if (conversationHistory.Count > keep)
            conversationHistory.RemoveRange(0, conversationHistory.Count - keep);
    }

    IEnumerator ClearSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (subtitleUI != null)
            subtitleUI.Clear();
    }

    void SetTalking(bool talking)
    {
        if (npcAnimator != null && !string.IsNullOrEmpty(talkingBoolParam))
            npcAnimator.SetBool(talkingBoolParam, talking);
    }

    IEnumerator StopTalkingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetTalking(false);
    }

    string GetSystemPromptByPersona()
    {
        if (scenarioManager == null) return empathicSystemPrompt;

        return scenarioManager.persona == PersonaType.Empathic
            ? empathicSystemPrompt
            : taskFocusedSystemPrompt;
    }

    string BuildUserMessage(string playerInput)
    {
        string rule =
@"Rules (strict):
- You MUST use ONLY the following names and roles (do not invent new suspects or names):
  Victim: Daniel (Project Lead of Project Aurora)
  Suspects: (1) Alex (Senior Systems Designer), (2) Project Manager, (3) Junior Programmer
- If the player's message is unrelated to the homicide investigation, respond briefly:
  'I’m not sure what you mean—let’s stay on the case.' Then give ONE concrete next step.
- If information is not provided in the context or the evidence list, say 'I don't have enough information yet' instead of guessing.
- Never reveal the killer outright. Guide the player.
- Keep the response concise (3–6 bullet points, <= 90 words).
";

        string phaseInstruction = "Task: Provide general investigation guidance.";

        if (progress != null)
        {
            switch (progress.phase)
            {
                case CasePhase.Overview:
                    phaseInstruction =
@"Task (Overview phase):
- Give a short case overview.
- Answer 'who are the suspects' by listing EXACTLY the three suspects and 1 motive each.
- Do NOT interpret evidence yet.
- End with 1–2 suggested areas to inspect next.";
                    break;

                case CasePhase.Investigate:
                case CasePhase.FocusOnClue:
                    phaseInstruction =
@"Task (Evidence phase):
- Use ONLY the inspected evidence list below.
- Explain what the evidence implies for timeline/opportunity/motive.
- Suggest 1–2 next evidence checks.
- Do NOT finalize the culprit.";
                    break;

                case CasePhase.FinalQuestion:
                    phaseInstruction =
@"Task (Final phase):
- Ask the player to name ONE suspect and explain WHY using evidence.
- Ask 1 follow-up question to test their reasoning.
- Do NOT confirm the correct answer unless the player states a clear conclusion.";
                    break;
            }
        }

        string evidenceBlock = "Inspected Evidence:\n(no evidence yet)";
        if (progress != null)
            evidenceBlock = "Inspected Evidence:\n" + progress.BuildEvidenceSummaryAll();

        return baseContext + "\n\n"
               + rule + "\n\n"
               + phaseInstruction + "\n\n"
               + evidenceBlock + "\n\n"
               + "Player: " + playerInput;
    }

    // --- AI-based final answer evaluation ---
    IEnumerator EvaluateConclusionWithAI(string playerInput)
    {
        string evalSystemPrompt =
@"You are the impartial judge of a murder mystery game. Evaluate the player's conclusion.

THE CORRECT ANSWER: Alex (Senior Systems Designer) committed the murder.

Key evidence that supports this:
- A3 (Wi-Fi Log): Alex's laptop reconnected to the office network at 10:23 PM — inside the 10–11 PM death window. Directly contradicts Alex's alibi.
- A2 vs P2 (Receipt cross-reference): Alex's taxi receipt is missing the Late Night Fee surcharge that appears on all rides after 9:00 PM (confirmed by the PM's receipt P2). Alex's alibi is fabricated.
- P3 (Remote Meeting Log): The Project Manager was on a verified video call 10:02–11:17 PM. Eliminated.
- J2 + J3 (Commit log + Chat log): The Junior Programmer was actively coding in response to real-time designer instructions throughout the death window. Could not have committed the murder. Eliminated.

VERDICT RULES — choose exactly ONE:
- VERDICT:CORRECT   → Player names Alex as the killer AND gives any reasoning touching the evidence (wifi, alibi, receipt, late night fee, timeline, fake receipt, or any of the above). Exact wording not required.
- VERDICT:NEED_EVIDENCE → Player names Alex as the killer but gives NO reasoning or evidence at all (e.g. just 'Alex did it').
- VERDICT:INCORRECT → Player names the wrong suspect, says Alex is innocent/cleared, or the logic is clearly backwards.

Respond in EXACTLY this two-line format:
VERDICT:<CORRECT|NEED_EVIDENCE|INCORRECT>
<One short in-character mentor sentence matching the verdict.>";

        bool done      = false;
        string verdict = "INCORRECT";
        string mentorReply = "I'm not convinced. Re-check the timeline and key evidence.";

        StartCoroutine(openAI.Send(
            evalSystemPrompt,
            "Player's statement: " + playerInput,
            onResult: (text) =>
            {
                Debug.Log($"[MentorNPC] AI verdict raw: {text}");
                foreach (var line in (text ?? "").Split('\n'))
                {
                    string t = line.Trim();
                    if (t.StartsWith("VERDICT:"))
                        verdict = t.Substring("VERDICT:".Length).Trim().ToUpper();
                    else if (!string.IsNullOrWhiteSpace(t))
                        mentorReply = t;
                }
                done = true;
            },
            onError: (err) =>
            {
                Debug.LogError($"[MentorNPC] AI evaluation error: {err}. Falling back to keyword check.");
                verdict     = IsCorrectConclusion(playerInput) ? "CORRECT" : "INCORRECT";
                mentorReply = verdict == "CORRECT"
                    ? "Your reasoning matches the evidence. Case closed."
                    : "I'm not convinced. Re-check the timeline and key evidence.";
                done = true;
            }
        ));

        yield return new WaitUntil(() => done);

        isBusy = false;

        bool isCorrect = verdict == "CORRECT";

        if (DataRecorder.Instance != null)
            DataRecorder.Instance.RecordFinalAnswer(playerInput, isCorrect);

        if (isCorrect)
        {
            progress.MarkResolved();
            Debug.Log("[CASE] Resolved via AI judgment.");
        }
        else if (verdict == "NEED_EVIDENCE")
        {
            Debug.Log("[CASE] Correct suspect but no evidence — NPC asking for reasoning.");
        }

        string fullReply = "Mentor: " + mentorReply;
        SetTalking(true);
        if (IsVoiceModality() && ttsManager != null)
        {
            subtitleUI.ShowPersistent(fullReply);
            ttsManager.Speak(mentorReply,
                onComplete: () =>
                {
                    SetTalking(false);
                    StartCoroutine(ClearSubtitleAfterDelay(1.0f));
                },
                onError: _ =>
                {
                    SetTalking(false);
                    subtitleUI?.Clear();
                }
            );
        }
        else
        {
            subtitleUI.Show(fullReply);
            StartCoroutine(StopTalkingAfterDelay(Mathf.Max(3f, mentorReply.Length * 0.05f)));
        }
    }

    // --- Final answer evaluation helpers ---
    bool LooksLikeConclusion(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        string s = input.ToLowerInvariant();
        return s.Contains("alex") || s.Contains("project manager") || s.Contains("pm") || s.Contains("junior");
    }

    bool IsCorrectConclusion(string input)
    {
        if (progress == null) return false;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.ToLowerInvariant();
        string killer = (progress.correctKillerName ?? "alex").ToLowerInvariant();

        if (!s.Contains(killer)) return false;

        // Require referencing at least one key aspect (ID or keyword), to avoid pure guessing.
        if (progress.evidenceRequiredForFinal != null && progress.evidenceRequiredForFinal.Length > 0)
        {
            foreach (var id in progress.evidenceRequiredForFinal)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (s.Contains(id.ToLowerInvariant())) return true;
            }
        }

        bool mentionsWifi = s.Contains("wifi") || s.Contains("wi-fi") || s.Contains("reconnect") || s.Contains("network");
        bool mentionsAlibi = s.Contains("alibi") || s.Contains("receipt") || s.Contains("taxi");
        bool mentionsTimeline = s.Contains("timeline") || s.Contains("time") || s.Contains("window");
        return mentionsWifi || mentionsAlibi || mentionsTimeline;
    }
}
