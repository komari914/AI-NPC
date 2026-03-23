using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubtitleUI : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;

    [Header("Paging (Split Long Text)")]
    [Tooltip("Max characters per page (rough). Reduce if still goes off-screen.")]
    public int maxCharsPerPage = 220;

    [Tooltip("Try to split at newlines or sentence ends for nicer paging.")]
    public bool smartSplit = true;

    [Header("Auto Duration")]
    public float baseSeconds = 1.2f;
    public float charsPerSecond = 18f;
    public float minSeconds = 2.5f;
    public float maxSeconds = 10f;

    [Header("Fade")]
    public float fadeSeconds = 0.2f;

    Coroutine routine;

    void Awake()
    {
        if (subtitleText == null)
            subtitleText = GetComponent<TextMeshProUGUI>();

        if (subtitleText != null)
        {
            subtitleText.enableWordWrapping = true;
            subtitleText.overflowMode = TextOverflowModes.Overflow;
            subtitleText.alignment = TextAlignmentOptions.TopLeft;
        }

        ClearInstant();
    }

    public void Show(string message)
    {
        if (subtitleText == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowPagedRoutine(message ?? ""));
    }

    /// <summary>
    /// Show subtitle and keep it visible until manually cleared (for voice mode)
    /// </summary>
    public void ShowPersistent(string message)
    {
        if (subtitleText == null) return;

        if (routine != null) StopCoroutine(routine);

        subtitleText.text = message;
        SetAlpha(1f);
    }

    /// <summary>
    /// Clear subtitle (call after TTS finishes)
    /// </summary>
    public void Clear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        ClearInstant();
    }

    IEnumerator ShowPagedRoutine(string message)
    {
        var pages = SplitIntoPages(message, maxCharsPerPage, smartSplit);

        for (int i = 0; i < pages.Count; i++)
        {
            string page = pages[i].Trim();

            // Optional: show page indicator
            if (pages.Count > 1)
                page = page + $"\n<size=70%><alpha=#AA>({i + 1}/{pages.Count})</alpha></size>";

            subtitleText.text = page;
            SetAlpha(1f);

            float stay = ComputeDuration(page);
            yield return new WaitForSeconds(stay);

            // small fade between pages (looks nicer)
            yield return FadeOut();
        }

        ClearInstant();
        routine = null;
    }

    float ComputeDuration(string s)
    {
        if (string.IsNullOrEmpty(s)) return minSeconds;
        float auto = baseSeconds + (s.Length / Mathf.Max(1f, charsPerSecond));
        return Mathf.Clamp(auto, minSeconds, maxSeconds);
    }

    IEnumerator FadeOut()
    {
        if (fadeSeconds <= 0f) yield break;

        float t = 0f;
        float startA = subtitleText.color.a;

        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, 0f, t / fadeSeconds);
            SetAlpha(a);
            yield return null;
        }
    }

    void SetAlpha(float a)
    {
        var c = subtitleText.color;
        c.a = a;
        subtitleText.color = c;
    }

    void ClearInstant()
    {
        if (subtitleText == null) return;
        subtitleText.text = "";
        SetAlpha(0f);
    }

    static List<string> SplitIntoPages(string text, int maxChars, bool smart)
    {
        var pages = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            pages.Add("");
            return pages;
        }

        text = text.Replace("\r", "");

        int start = 0;
        while (start < text.Length)
        {
            int len = Mathf.Min(maxChars, text.Length - start);
            int end = start + len;

            if (smart && end < text.Length)
            {
                // Prefer splitting at newline
                int nl = text.LastIndexOf('\n', end - 1, len);
                if (nl > start + 40) // avoid tiny pages
                {
                    end = nl + 1;
                }
                else
                {
                    // Prefer splitting at sentence end
                    int dot = LastIndexOfAny(text, end - 1, len, ".!?");
                    if (dot > start + 60) end = dot + 1;
                    else
                    {
                        // Prefer splitting at space
                        int sp = text.LastIndexOf(' ', end - 1, len);
                        if (sp > start + 60) end = sp + 1;
                    }
                }
            }

            string chunk = text.Substring(start, end - start);
            pages.Add(chunk);
            start = end;
        }

        return pages;
    }

    static int LastIndexOfAny(string s, int startIndex, int count, string chars)
    {
        int min = startIndex - count + 1;
        for (int i = startIndex; i >= min; i--)
        {
            if (i < 0 || i >= s.Length) continue;
            if (chars.IndexOf(s[i]) >= 0) return i;
        }
        return -1;
    }
}
