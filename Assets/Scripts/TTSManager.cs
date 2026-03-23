using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    [Header("API Settings")]
    [Tooltip("OpenAI API key for TTS")]
    public string openAIApiKey = "";

    [Header("TTS Settings")]
    [Tooltip("Voice to use: alloy, echo, fable, onyx, nova, shimmer")]
    public string voiceType = "alloy";

    [Tooltip("Model: tts-1 (faster) or tts-1-hd (higher quality)")]
    public string model = "tts-1";

    [Tooltip("Speech speed (0.25 to 4.0, 1.0 is normal)")]
    [Range(0.25f, 4.0f)]
    public float speed = 1.0f;

    [Header("Audio Settings")]
    public AudioSource audioSource;

    [Tooltip("Volume for TTS playback")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("State")]
    public bool isSpeaking = false;
    public bool isProcessing = false;

    // API endpoint
    private const string TTSEndpoint = "https://api.openai.com/v1/audio/speech";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Setup audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    void Update()
    {
        // Update speaking state
        isSpeaking = audioSource != null && audioSource.isPlaying;

        // Update volume
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    /// <summary>
    /// Speak text using TTS
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="onComplete">Callback when speech is done</param>
    /// <param name="onError">Callback on error</param>
    public void Speak(string text, Action onComplete = null, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[TTS] Empty text provided");
            onError?.Invoke("Empty text");
            return;
        }

        // Only use TTS in Voice modality
        if (ScenarioManager.Instance != null &&
            ScenarioManager.Instance.modality != ModalityType.Voice)
        {
            Debug.Log("[TTS] Not in Voice modality, skipping TTS");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SpeakCoroutine(text, onComplete, onError));
    }

    IEnumerator SpeakCoroutine(string text, Action onComplete, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(openAIApiKey))
        {
            string error = "OpenAI API key is empty!";
            Debug.LogError($"[TTS] {error}");
            onError?.Invoke(error);
            yield break;
        }

        // Stop any current speech
        StopSpeaking();

        isProcessing = true;

        // Create request body
        TTSRequest requestBody = new TTSRequest
        {
            model = this.model,
            input = text,
            voice = voiceType,
            speed = this.speed
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        Debug.Log($"[TTS] Requesting speech for: {text.Substring(0, Mathf.Min(50, text.Length))}...");

        using (UnityWebRequest request = new UnityWebRequest(TTSEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

            yield return request.SendWebRequest();

            isProcessing = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioData = request.downloadHandler.data;
                Debug.Log($"[TTS] Received audio data: {audioData.Length} bytes");

                // Convert MP3 bytes to AudioClip
                // Note: Unity doesn't natively support MP3 loading at runtime
                // We need to use a workaround or convert to WAV
                // For now, we'll save to temp file and load using UnityWebRequest
                StartCoroutine(PlayAudioFromBytes(audioData, onComplete, onError));
            }
            else
            {
                string error = $"TTS API error: {request.error}\n{request.downloadHandler.text}";
                Debug.LogError($"[TTS] {error}");
                onError?.Invoke(error);
            }
        }
    }

    IEnumerator PlayAudioFromBytes(byte[] audioData, Action onComplete, Action<string> onError)
    {
        // Save to temporary file
        string tempPath = Path.Combine(Application.temporaryCachePath, "tts_temp.mp3");

        try
        {
            File.WriteAllBytes(tempPath, audioData);
            Debug.Log($"[TTS] Saved audio to: {tempPath}");
        }
        catch (Exception e)
        {
            string error = $"Failed to save audio file: {e.Message}";
            Debug.LogError($"[TTS] {error}");
            onError?.Invoke(error);
            yield break;
        }

        // Load audio using UnityWebRequestMultimedia
        string fileUrl = "file://" + tempPath;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                if (clip != null)
                {
                    Debug.Log($"[TTS] Playing audio clip (length: {clip.length}s)");
                    audioSource.clip = clip;
                    audioSource.Play();

                    // Wait for audio to finish
                    yield return new WaitWhile(() => audioSource.isPlaying);

                    Debug.Log("[TTS] Speech finished");
                    onComplete?.Invoke();
                }
                else
                {
                    string error = "Failed to create AudioClip from file";
                    Debug.LogError($"[TTS] {error}");
                    onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"Failed to load audio file: {www.error}";
                Debug.LogError($"[TTS] {error}");
                onError?.Invoke(error);
            }
        }

        // Clean up temp file
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TTS] Failed to delete temp file: {e.Message}");
        }
    }

    /// <summary>
    /// Stop current speech
    /// </summary>
    public void StopSpeaking()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[TTS] Stopped speaking");
        }
    }

    /// <summary>
    /// Check if currently speaking or processing
    /// </summary>
    public bool IsBusy()
    {
        return isSpeaking || isProcessing;
    }

    [Serializable]
    private class TTSRequest
    {
        public string model;
        public string input;
        public string voice;
        public float speed;
    }
}
