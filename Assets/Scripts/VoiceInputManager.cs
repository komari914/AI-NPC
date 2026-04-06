using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class VoiceInputManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("ElevenLabs API key (xi-api-key) for Speech-to-Text (Scribe)")]
    public string elevenLabsApiKey = "";

    [Header("Recording Settings")]
    [Tooltip("Maximum recording duration in seconds")]
    public float maxRecordingDuration = 10f;

    [Tooltip("Recording sample rate")]
    public int sampleRate = 44100;

    [Header("Input Settings")]
    [Tooltip("Key to hold for recording (default: V)")]
    public Key recordKey = Key.V;

    [Tooltip("Alternative: toggle recording mode")]
    public bool toggleMode = false;

    [Header("UI References")]
    public GameObject recordingIndicator;
    public TextMeshProUGUI recordingText;
    public TextMeshProUGUI statusText;

    [Header("References")]
    public MentorNPC mentorNPC;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public bool playbackRecording = false; // For debugging

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void StartWebGLRecording();
    [DllImport("__Internal")] static extern void StopWebGLRecording(string apiKey, string goName, string callback);
#endif

    // State
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isProcessing = false;
    private float recordingStartTime;

    // ElevenLabs STT endpoint (Scribe)
    private const string STTEndpoint = "https://api.elevenlabs.io/v1/speech-to-text";

    void Start()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        HideRecordingIndicator();
        StartCoroutine(InitMicrophone());
    }

    IEnumerator InitMicrophone()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        yield return null;
        if (UnityEngine.Microphone.devices.Length > 0)
        {
            microphoneDevice = UnityEngine.Microphone.devices[0];
            Debug.Log($"[VoiceInput] Using microphone: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("[VoiceInput] No microphone found!");
            UpdateStatus("No microphone found.");
        }
#else
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogError("[VoiceInput] Microphone permission denied.");
            UpdateStatus("Microphone permission denied.");
            yield break;
        }
        // WebGL microphone initialised via browser — device name not needed
        microphoneDevice = "";
        Debug.Log("[VoiceInput] WebGL microphone authorised.");
#endif
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Only allow voice input in Voice modality
        if (ScenarioManager.Instance != null &&
            ScenarioManager.Instance.modality != ModalityType.Voice)
        {
            return;
        }

        if (DialogueInputUI.Instance != null && DialogueInputUI.Instance.IsOpen) return;

        if (toggleMode)
        {
            // Toggle mode: press once to start, press again to stop
            if (Keyboard.current[recordKey].wasPressedThisFrame)
            {
                if (isRecording)
                {
                    StopRecording();
                }
                else
                {
                    StartRecording();
                }
            }
        }
        else
        {
            // Push-to-talk mode: hold to record
            if (Keyboard.current[recordKey].wasPressedThisFrame)
            {
                StartRecording();
            }
            else if (Keyboard.current[recordKey].wasReleasedThisFrame)
            {
                StopRecording();
            }
        }

        // Update recording time display
        if (isRecording && recordingText != null)
        {
            float elapsed = Time.time - recordingStartTime;
            recordingText.text = $"Recording... {elapsed:F1}s / {maxRecordingDuration:F0}s";

            // Auto-stop if max duration reached
            if (elapsed >= maxRecordingDuration)
            {
                StopRecording();
            }
        }
    }

    void StartRecording()
    {
        if (isRecording || isProcessing) return;

        // Check NPC proximity before recording
        if (mentorNPC != null && !mentorNPC.IsPlayerCloseEnough())
        {
            UpdateStatus("Move closer to the mentor first!");
            Debug.Log("[VoiceInput] Recording blocked: player too far from NPC.");
            return;
        }

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogError("[VoiceInput] No microphone available!");
            UpdateStatus("No microphone found!");
            return;
        }

        Debug.Log("[VoiceInput] Starting recording...");
        isRecording = true;
        recordingStartTime = Time.time;

#if UNITY_WEBGL && !UNITY_EDITOR
        StartWebGLRecording();
#else
        recordedClip = UnityEngine.Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, sampleRate);
#endif
        ShowRecordingIndicator();
        UpdateStatus("Listening...");
    }

    void StopRecording()
    {
        if (!isRecording) return;

        Debug.Log("[VoiceInput] Stopping recording...");
        isRecording = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // JS plugin handles recording, STT, and calls OnWebGLTranscription when done
        isProcessing = true;
        UpdateStatus("Processing...");
        HideRecordingIndicator();
        StopWebGLRecording(elevenLabsApiKey, gameObject.name, nameof(OnWebGLTranscription));
        return; // JS takes over from here
#else
        int lastSample = UnityEngine.Microphone.GetPosition(microphoneDevice);
        UnityEngine.Microphone.End(microphoneDevice);

        HideRecordingIndicator();

        // Trim audio clip to actual recorded length
        if (recordedClip != null && lastSample > 0)
        {
            AudioClip trimmedClip = TrimAudioClip(recordedClip, lastSample);

            // Optional: playback for debugging
            if (playbackRecording && audioSource != null)
            {
                audioSource.clip = trimmedClip;
                audioSource.Play();
            }

            // Send to ElevenLabs STT
            StartCoroutine(TranscribeAudio(trimmedClip));
        }
        else
        {
            Debug.LogWarning("[VoiceInput] No audio recorded");
            UpdateStatus("No audio recorded");
        }
#endif
    }

    AudioClip TrimAudioClip(AudioClip clip, int samples)
    {
        float[] data = new float[samples * clip.channels];
        clip.GetData(data, 0);

        AudioClip trimmed = AudioClip.Create(
            clip.name + "_trimmed",
            samples,
            clip.channels,
            clip.frequency,
            false
        );
        trimmed.SetData(data, 0);

        return trimmed;
    }

    IEnumerator TranscribeAudio(AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(elevenLabsApiKey))
        {
            UpdateStatus("Error: API key not set");
            Debug.LogError("[VoiceInput] ElevenLabs API key is empty!");
            yield break;
        }

        isProcessing = true;
        UpdateStatus("Processing...");

        // Convert AudioClip to WAV bytes
        byte[] wavData = ConvertAudioClipToWav(clip);

        // ElevenLabs Scribe multipart form
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model_id", "scribe_v1");

        using (UnityWebRequest request = UnityWebRequest.Post(STTEndpoint, form))
        {
            request.SetRequestHeader("xi-api-key", elevenLabsApiKey);

            yield return request.SendWebRequest();

            isProcessing = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"[VoiceInput] Whisper response: {response}");

                try
                {
                    // ElevenLabs Scribe returns { "text": "...", ... }
                    STTResponse whisperResponse = JsonUtility.FromJson<STTResponse>(response);
                    string transcription = whisperResponse.text.Trim();

                    Debug.Log($"[VoiceInput] Transcription: {transcription}");
                    UpdateStatus($"You said: {transcription}");

                    // Send to MentorNPC
                    if (string.IsNullOrWhiteSpace(transcription))
                    {
                        Debug.LogWarning("[VoiceInput] Transcription was empty — nothing sent to NPC.");
                    }
                    else if (mentorNPC == null)
                    {
                        Debug.LogError("[VoiceInput] mentorNPC reference is null! Assign it in the Inspector.");
                    }
                    else
                    {
                        Debug.Log($"[VoiceInput] Sending transcription to MentorNPC: \"{transcription}\"");
                        mentorNPC.TalkWithPlayerInput(transcription);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VoiceInput] Failed to parse response: {e.Message}\n{response}");
                    UpdateStatus("Error: Failed to process audio");
                }
            }
            else
            {
                Debug.LogError($"[VoiceInput] ElevenLabs STT error: {request.error}\n{request.downloadHandler.text}");
                UpdateStatus($"Error: {request.error}");
            }
        }
    }

    byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];

        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (MemoryStream stream = new MemoryStream())
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples_count = samples.Length;

            // Write WAV header
            stream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            stream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(stream.Length - 8);
            stream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            stream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            stream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            stream.Write(subChunk1, 0, 4);

            UInt16 one = 1;
            Byte[] audioFormat = BitConverter.GetBytes(one);
            stream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            stream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            stream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
            stream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            stream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            stream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples_count * channels * 2);
            stream.Write(subChunk2, 0, 4);

            stream.Write(bytesData, 0, bytesData.Length);

            return stream.ToArray();
        }
    }

    void ShowRecordingIndicator()
    {
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(true);
        }
    }

    void HideRecordingIndicator()
    {
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(false);
        }

        if (recordingText != null)
        {
            recordingText.text = "Hold V to speak";
        }
    }

    // Called by JS plugin via SendMessage after WebGL transcription completes
    public void OnWebGLTranscription(string text)
    {
        isProcessing = false;
        text = (text ?? "").Trim();
        UpdateStatus(string.IsNullOrEmpty(text) ? "No speech detected." : $"You said: {text}");

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[VoiceInput] WebGL transcription empty.");
            return;
        }

        if (mentorNPC == null)
        {
            Debug.LogError("[VoiceInput] mentorNPC is null.");
            return;
        }

        Debug.Log($"[VoiceInput] WebGL transcription: {text}");
        mentorNPC.TalkWithPlayerInput(text);
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log($"[VoiceInput] Status: {message}");
    }

    [Serializable]
    private class STTResponse
    {
        public string text;   // ElevenLabs Scribe response field
    }
}
