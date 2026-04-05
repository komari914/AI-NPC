using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Text-to-Speech using ElevenLabs API.
/// Requests PCM audio and builds AudioClip directly from bytes — no file I/O, WebGL compatible.
/// Endpoint: POST https://api.elevenlabs.io/v1/text-to-speech/{voice_id}
/// </summary>
public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    [Header("ElevenLabs API")]
    public string elevenLabsApiKey = "";

    [Tooltip("Voice ID from ElevenLabs dashboard")]
    public string voiceId = "21m00Tcm4TlvDq8ikWAM";

    [Tooltip("eleven_turbo_v2_5 (fast) or eleven_multilingual_v2 (quality)")]
    public string modelId = "eleven_turbo_v2_5";

    [Header("Voice Settings")]
    [Range(0f, 1f)] public float stability       = 0.5f;
    [Range(0f, 1f)] public float similarityBoost = 0.75f;
    [Range(0f, 1f)] public float style           = 0.0f;
    public bool useSpeakerBoost = true;

    [Header("Audio")]
    public AudioSource audioSource;
    [Range(0f, 1f)] public float volume = 1.0f;

    [Header("State")]
    public bool isSpeaking   = false;
    public bool isProcessing = false;

    // PCM 22050 Hz mono — supported on all platforms including WebGL
    private const string TTSEndpoint  = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string OutputFormat = "pcm_22050";
    private const int    SampleRate   = 22050;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.volume      = volume;
    }

    void Update()
    {
        isSpeaking = audioSource != null && audioSource.isPlaying;
        if (audioSource != null) audioSource.volume = volume;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void Speak(string text, Action onComplete = null, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(text)) { onError?.Invoke("Empty text"); return; }

        // Strip speaker prefixes like "Mentor:", "NPC:" etc.
        int colon = text.IndexOf(':');
        if (colon > 0 && colon < 20)
            text = text.Substring(colon + 1).TrimStart();

        // Only play TTS in Voice modality
        if (ScenarioManager.Instance != null &&
            ScenarioManager.Instance.modality != ModalityType.Voice)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SpeakCoroutine(text, onComplete, onError));
    }

    public void StopSpeaking()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool IsBusy() => isSpeaking || isProcessing;

    // ─── Coroutine ────────────────────────────────────────────────────────────

    IEnumerator SpeakCoroutine(string text, Action onComplete, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(elevenLabsApiKey))
        {
            string err = "[TTS] ElevenLabs API key is empty!";
            Debug.LogError(err); onError?.Invoke(err); yield break;
        }

        StopSpeaking();
        isProcessing = true;

        string json = $"{{" +
                      $"\"text\":\"{EscapeJson(text)}\"," +
                      $"\"model_id\":\"{modelId}\"," +
                      $"\"voice_settings\":{{" +
                      $"\"stability\":{stability.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"similarity_boost\":{similarityBoost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"style\":{style.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"use_speaker_boost\":{(useSpeakerBoost ? "true" : "false")}" +
                      $"}}}}";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        string url     = $"{TTSEndpoint}{voiceId}?output_format={OutputFormat}";

        Debug.Log($"[TTS] Requesting: {text.Substring(0, Mathf.Min(60, text.Length))}...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("xi-api-key",   elevenLabsApiKey);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept",       "audio/pcm");

            yield return request.SendWebRequest();

            isProcessing = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[]    pcmBytes = request.downloadHandler.data;
                AudioClip clip     = PcmBytesToAudioClip(pcmBytes, SampleRate);

                if (clip != null)
                {
                    Debug.Log($"[TTS] Playing {clip.length:F1}s clip");
                    audioSource.clip = clip;
                    audioSource.Play();
                    yield return new WaitWhile(() => audioSource.isPlaying);
                    onComplete?.Invoke();
                }
                else
                {
                    string err = "[TTS] Failed to build AudioClip from PCM";
                    Debug.LogError(err); onError?.Invoke(err);
                }
            }
            else
            {
                string err = $"[TTS] API error: {request.error} — {request.downloadHandler.text}";
                Debug.LogError(err); onError?.Invoke(err);
            }
        }
    }

    // ─── PCM → AudioClip (no file I/O, works on WebGL) ───────────────────────

    static AudioClip PcmBytesToAudioClip(byte[] pcmBytes, int sampleRate)
    {
        if (pcmBytes == null || pcmBytes.Length < 2) return null;

        // ElevenLabs PCM is 16-bit signed little-endian, mono
        int sampleCount = pcmBytes.Length / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short s = (short)(pcmBytes[i * 2] | (pcmBytes[i * 2 + 1] << 8));
            samples[i] = s / 32768f;
        }

        AudioClip clip = AudioClip.Create("tts", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "\\r");
}
