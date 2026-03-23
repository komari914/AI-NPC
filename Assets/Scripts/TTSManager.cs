using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Text-to-Speech using ElevenLabs API.
/// Endpoint: POST https://api.elevenlabs.io/v1/text-to-speech/{voice_id}
/// </summary>
public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    [Header("ElevenLabs API")]
    [Tooltip("ElevenLabs API key (xi-api-key)")]
    public string elevenLabsApiKey = "";

    [Tooltip("Voice ID from ElevenLabs (e.g. 'Rachel' = 21m00Tcm4TlvDq8ikWAM)")]
    public string voiceId = "21m00Tcm4TlvDq8ikWAM";

    [Tooltip("Model: eleven_turbo_v2_5 (fast) or eleven_multilingual_v2 (high quality)")]
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

    private const string TTSEndpoint = "https://api.elevenlabs.io/v1/text-to-speech/";

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
        if (string.IsNullOrWhiteSpace(text))
        {
            onError?.Invoke("Empty text");
            return;
        }

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
        {
            audioSource.Stop();
            Debug.Log("[TTS] Stopped speaking");
        }
    }

    public bool IsBusy() => isSpeaking || isProcessing;

    // ─── Coroutines ───────────────────────────────────────────────────────────

    IEnumerator SpeakCoroutine(string text, Action onComplete, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(elevenLabsApiKey))
        {
            string err = "[TTS] ElevenLabs API key is empty!";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        StopSpeaking();
        isProcessing = true;

        // Build JSON body manually to handle nested voice_settings
        string json = $"{{" +
                      $"\"text\":\"{EscapeJson(text)}\"," +
                      $"\"model_id\":\"{modelId}\"," +
                      $"\"voice_settings\":{{" +
                      $"\"stability\":{stability.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"similarity_boost\":{similarityBoost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"style\":{style.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"\"use_speaker_boost\":{(useSpeakerBoost ? "true" : "false")}" +
                      $"}}" +
                      $"}}";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        string url     = TTSEndpoint + voiceId + "?output_format=mp3_44100_128";

        Debug.Log($"[TTS] Requesting speech (ElevenLabs): {text.Substring(0, Mathf.Min(60, text.Length))}...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("xi-api-key",    elevenLabsApiKey);
            request.SetRequestHeader("Content-Type",  "application/json");
            request.SetRequestHeader("Accept",        "audio/mpeg");

            yield return request.SendWebRequest();

            isProcessing = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioData = request.downloadHandler.data;
                Debug.Log($"[TTS] Received {audioData.Length} bytes of audio");
                StartCoroutine(PlayAudioFromBytes(audioData, onComplete, onError));
            }
            else
            {
                string err = $"[TTS] API error: {request.error} — {request.downloadHandler.text}";
                Debug.LogError(err);
                onError?.Invoke(err);
            }
        }
    }

    IEnumerator PlayAudioFromBytes(byte[] audioData, Action onComplete, Action<string> onError)
    {
        string tempPath = Path.Combine(Application.temporaryCachePath, "tts_elevenlabs.mp3");

        try
        {
            File.WriteAllBytes(tempPath, audioData);
        }
        catch (Exception e)
        {
            string err = $"[TTS] Failed to save temp file: {e.Message}";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    Debug.Log($"[TTS] Playing clip ({clip.length:F1}s)");
                    audioSource.clip = clip;
                    audioSource.Play();
                    yield return new WaitWhile(() => audioSource.isPlaying);
                    Debug.Log("[TTS] Playback finished");
                    onComplete?.Invoke();
                }
                else
                {
                    string err = "[TTS] Failed to decode AudioClip";
                    Debug.LogError(err);
                    onError?.Invoke(err);
                }
            }
            else
            {
                string err = $"[TTS] Failed to load audio: {www.error}";
                Debug.LogError(err);
                onError?.Invoke(err);
            }
        }

        try { if (File.Exists(tempPath)) File.Delete(tempPath); }
        catch (Exception e) { Debug.LogWarning($"[TTS] Could not delete temp file: {e.Message}"); }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "\\r");
}
