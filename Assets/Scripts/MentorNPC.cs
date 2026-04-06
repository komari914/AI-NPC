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

IMPORTANT: You only know what the player has already inspected. The 'Inspected Evidence' list below is everything you and the player currently have access to. Do NOT reference, hint at, or discuss any evidence that is not in that list. If the player asks about something you have no evidence for, say you haven't found anything on that yet and suggest they keep looking.

You are the player's mentor / team leader. Guide reasoning from collected evidence only. Do not reveal the killer directly.
";

    [Header("Evidence Threshold (to close the case)")]
    [Tooltip("Evidence IDs considered 'key' — player must collect this many before the case can be solved.")]
    public string[] keyEvidenceIds = { "A2", "A3", "P2", "P3", "J2", "J3" };

    [Tooltip("How many key evidence items must be collected before the player can close the case.")]
    public int minKeyEvidenceToConvict = 3;

    [Header("Fallback Question (used if no input UI is assigned)")]
    [TextArea(2, 6)]
    public string fallbackPlayerQuestion =
        "Can you give me an overview of the case and the suspects?";

    [Header("Conversation History")]
    [Tooltip("Max turns kept in memory (each turn = 1 user + 1 assistant message)")]
    public int maxHistoryTurns = 6;

    private bool isBusy = false;
    private bool openingFinished = false;
    private bool _openingStarted = false;
    private string latestPlayerInput = "";
    private readonly List<ChatMessage> conversationHistory = new();

    // Two-step accusation state:
    // non-null = player has named a suspect, waiting for their evidence explanation
    private string awaitingEvidenceForSuspect = null;

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
        {
            _openingStarted = true;
            StartCoroutine(PlayOpeningRoutine());
        }
        else if (!playOpeningOnStart)
        {
            // Opening will be triggered manually by ControlsTutorialUI
        }
        else
        {
            _openingStarted  = true;
            openingFinished  = true;
            progress?.MarkOpeningFinished();
        }
    }

    /// <summary>Called by ControlsTutorialUI after the player dismisses the controls panel.</summary>
    public void StartOpeningManually()
    {
        if (_openingStarted) return; // already playing — do nothing
        _openingStarted = true;

        if (subtitleUI != null)
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
        if (!Keyboard.current.vKey.wasPressedThisFrame) return;
        if (DialogueInputUI.Instance != null && DialogueInputUI.Instance.IsOpen) return;

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

        // FinalQuestion phase: player giving up → unresolved ending
        if (progress != null && progress.phase == CasePhase.FinalQuestion
            && awaitingEvidenceForSuspect == null
            && LooksLikeGivingUp(latestPlayerInput))
        {
            TriggerUnresolvedEnding();
            return;
        }

        // Step 2: player is explaining evidence for their accused suspect → evaluate
        if (awaitingEvidenceForSuspect != null)
        {
            string combined = $"Suspect: {awaitingEvidenceForSuspect}. Evidence: {latestPlayerInput}";
            awaitingEvidenceForSuspect = null;
            subtitleUI.Show("Mentor: (evaluating your reasoning...)");
            isBusy = true;
            StartCoroutine(EvaluateConclusionWithAI(combined));
            return;
        }

        // Step 1: player names a suspect → ask WHY, don't evaluate yet
        if (LooksLikeConclusion(latestPlayerInput))
        {
            awaitingEvidenceForSuspect = ExtractSuspect(latestPlayerInput);
            AskForEvidence(awaitingEvidenceForSuspect);
            return;
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
                // Store only the player's actual words (not the full context).
                // Context is rebuilt fresh in every BuildUserMessage call.
                conversationHistory.Add(new ChatMessage("user",      latestPlayerInput));
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

                case CasePhase.FinalQuestion:
                    phaseInstruction =
@"Task (Final phase — time is running out):
- You just asked the player if they have found the killer. They are responding now.
- If they name a suspect, acknowledge and ask them to back it up with evidence.
- If they say they haven't found the killer or don't know, respond with understanding and wrap up.
- Do NOT confirm the correct answer. Keep the response brief (1–2 sentences).";
                    break;

                case CasePhase.Investigate:
                case CasePhase.FocusOnClue:
                    phaseInstruction =
@"Task (Evidence phase):
- Discuss ONLY the evidence listed under 'Inspected Evidence' below. Never mention evidence not in that list.
- Explain what each collected item implies about timeline, opportunity, or motive.
- If the player asks about something not yet found, say 'We haven't found anything on that yet — keep looking.'
- If the player directly asks who the killer is (e.g. 'Is it Alex?', 'Did the PM do it?'), do NOT answer. Instead say: 'We're not ready to draw conclusions yet. Let's gather more evidence first.' Then suggest one concrete next step.
- Do NOT finalize or confirm the culprit under any circumstances during this phase.";
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

    // --- Evidence threshold helper ---
    int CountKeyEvidenceCollected()
    {
        if (progress == null || keyEvidenceIds == null) return 0;
        int count = 0;
        foreach (var id in keyEvidenceIds)
            if (progress.HasClue(id)) count++;
        return count;
    }

    // --- AI-based final answer evaluation ---
    IEnumerator EvaluateConclusionWithAI(string playerInput)
    {
        // Check how many key evidence items the player has actually collected
        int collected = CountKeyEvidenceCollected();
        if (collected < minKeyEvidenceToConvict)
        {
            isBusy = false;
            awaitingEvidenceForSuspect = ExtractSuspect(playerInput);

            string feedback = collected == 0
                ? "That's an interesting lead, but you haven't collected enough evidence yet. Keep searching the scene."
                : $"Good observation — that piece of evidence is relevant. But one clue isn't enough to close the case. You've found {collected} of the key pieces. Keep investigating.";

            ShowMentorReply(feedback);
            yield break;
        }

        string evalSystemPrompt =
@"You are the impartial judge of a murder mystery game. The player has collected enough evidence and is now explaining why Alex is the killer. Evaluate whether their explanation covers multiple evidence points — one clue alone is not sufficient.

THE CORRECT ANSWER: Alex (Senior Systems Designer) committed the murder.

Key evidence chains that prove Alex is guilty:
- A3 (Wi-Fi Log): Alex's laptop reconnected at 10:23 PM — inside the death window. Disproves his alibi.
- A2 vs P2 (Fake receipt): Alex's taxi receipt has no Late Night Fee surcharge, but all rides after 9 PM have this fee (confirmed by PM's receipt P2). Alex's alibi is fabricated.
- P3 (PM alibi): PM was on a verified video call 10:02–11:17 PM. Eliminated.
- J2 + J3 (JP alibi): Junior Programmer was actively coding throughout the death window. Eliminated.

VERDICT RULES — choose exactly ONE:
- VERDICT:CORRECT    → Player accuses Alex AND references AT LEAST TWO distinct evidence points (e.g. both the fake receipt AND the wifi log, or the fake receipt AND an eliminated suspect). Single-clue explanations are NOT enough.
- VERDICT:NEED_EVIDENCE → Player accuses Alex with only ONE piece of evidence, or explains vaguely without referencing specific clues.
- VERDICT:INCORRECT  → Player accuses the wrong suspect, or their logic clears Alex.

Respond in EXACTLY this two-line format:
VERDICT:<CORRECT|NEED_EVIDENCE|INCORRECT>
<One short in-character mentor sentence — for CORRECT say the case is solved; for NEED_EVIDENCE name what additional evidence is still missing; for INCORRECT say the evidence doesn't hold up.>";

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
            progress?.MarkResolved();
            Debug.Log("[CASE] Resolved via AI judgment.");
        }
        else if (verdict == "NEED_EVIDENCE")
        {
            // Evidence too weak — keep waiting for better explanation
            awaitingEvidenceForSuspect = ExtractSuspect(playerInput);
            Debug.Log("[CASE] Evidence insufficient — re-asking for evidence.");
        }
        else
        {
            // INCORRECT — clear accusation state, player goes back to investigating
            awaitingEvidenceForSuspect = null;
            Debug.Log("[CASE] Incorrect reasoning — back to investigating.");
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

    // --- Shared reply display helper ---
    void ShowMentorReply(string reply)
    {
        SetTalking(true);
        if (IsVoiceModality() && ttsManager != null)
        {
            subtitleUI.ShowPersistent("Mentor: " + reply);
            ttsManager.Speak(reply,
                onComplete: () => { SetTalking(false); StartCoroutine(ClearSubtitleAfterDelay(1f)); },
                onError:    _ => { SetTalking(false); subtitleUI?.Clear(); }
            );
        }
        else
        {
            subtitleUI.Show("Mentor: " + reply);
            StartCoroutine(StopTalkingAfterDelay(Mathf.Max(3f, reply.Length * 0.05f)));
        }
    }

    // --- Two-step accusation helpers ---

    void AskForEvidence(string suspect)
    {
        string reply = $"Interesting. What makes you think it's {suspect}? Walk me through the evidence.";
        SetTalking(true);

        if (IsVoiceModality() && ttsManager != null)
        {
            subtitleUI.ShowPersistent("Mentor: " + reply);
            ttsManager.Speak(reply,
                onComplete: () => { SetTalking(false); StartCoroutine(ClearSubtitleAfterDelay(0.5f)); },
                onError:    _ => { SetTalking(false); subtitleUI?.Clear(); }
            );
        }
        else
        {
            subtitleUI.Show("Mentor: " + reply);
            StartCoroutine(StopTalkingAfterDelay(Mathf.Max(3f, reply.Length * 0.05f)));
        }
    }

    string ExtractSuspect(string input)
    {
        string s = input.ToLowerInvariant();
        if (s.Contains("alex"))                              return "Alex";
        if (s.Contains("project manager") || s.Contains("pm")) return "the Project Manager";
        if (s.Contains("junior") || s.Contains("jp"))       return "the Junior Programmer";
        return "this suspect";
    }

    // --- FinalQuestion phase: proactive mentor prompt ---

    /// <summary>Called by TimerManager when the warning fires.</summary>
    public void StartFinalPhasePrompt()
    {
        if (!openingFinished) return;
        StartCoroutine(PlayFinalPhasePromptRoutine());
    }

    IEnumerator PlayFinalPhasePromptRoutine()
    {
        // Wait until the NPC finishes both the API call and any ongoing TTS
        yield return new WaitUntil(() => !isBusy && (ttsManager == null || !ttsManager.IsBusy()));

        string prompt = "Time is almost up. Have you figured out who killed Daniel — and why?";
        SetTalking(true);

        if (IsVoiceModality() && ttsManager != null)
        {
            subtitleUI.ShowPersistent("Mentor: " + prompt);
            bool done = false;
            ttsManager.Speak(prompt,
                onComplete: () => { done = true; },
                onError:    _ => { done = true; }
            );
            yield return new WaitUntil(() => done);
            subtitleUI.Clear();
        }
        else
        {
            subtitleUI.Show("Mentor: " + prompt);
            yield return new WaitForSeconds(Mathf.Max(3f, prompt.Length * 0.05f));
        }

        SetTalking(false);
    }

    // --- Unresolved ending ---

    void TriggerUnresolvedEnding()
    {
        if (DataRecorder.Instance != null)
            DataRecorder.Instance.RecordFinalAnswer("(gave up)", false);

        string reply = "Understood. We'll close the case for now. Good work today.";

        SetTalking(true);
        if (IsVoiceModality() && ttsManager != null)
        {
            subtitleUI.ShowPersistent("Mentor: " + reply);
            ttsManager.Speak(reply,
                onComplete: () => { SetTalking(false); ShowGameEnd(false); },
                onError:    _ => { SetTalking(false); ShowGameEnd(false); }
            );
        }
        else
        {
            subtitleUI.Show("Mentor: " + reply);
            StartCoroutine(ShowGameEndAfterDelay(Mathf.Max(3f, reply.Length * 0.05f), false));
        }
    }

    void ShowGameEnd(bool success)
    {
        float elapsed = TimerManager.Instance != null ? TimerManager.Instance.timeElapsed : 0f;
        GameEndUI endUI = Object.FindFirstObjectByType<GameEndUI>();
        string msg = success ? "Case solved!" : "Case closed — killer not identified.";
        endUI?.ShowGameEnd(success, elapsed, msg);
    }

    IEnumerator ShowGameEndAfterDelay(float delay, bool success)
    {
        yield return new WaitForSeconds(delay);
        SetTalking(false);
        ShowGameEnd(success);
    }

    // --- Final answer evaluation helpers ---
    bool LooksLikeConclusion(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        string s = input.ToLowerInvariant();
        return s.Contains("alex") || s.Contains("project manager") || s.Contains("pm") || s.Contains("junior");
    }

    bool LooksLikeGivingUp(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        string s = input.ToLowerInvariant();
        return s.Contains("not yet") || s.Contains("don't know") || s.Contains("no idea") ||
               s.Contains("haven't") || s.Contains("give up") || s.Contains("no clue") ||
               s.Contains("still looking") || s.Contains("can't figure") ||
               s == "no" || s == "nope" || s == "idk";
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
