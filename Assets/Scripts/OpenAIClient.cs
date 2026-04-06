using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ChatMessage
{
    public string role;    // "user" or "assistant"
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role    = role;
        this.content = content;
    }
}

[Serializable]
public class OpenAIResponsesApiResult
{
    public OutputItem[] output;

    [Serializable]
    public class OutputItem
    {
        public ContentItem[] content;
    }

    [Serializable]
    public class ContentItem
    {
        public string type; // "output_text"
        public string text;
    }
}

public class OpenAIClient : MonoBehaviour
{
    [Header("API (TEST ONLY)")]
    [Tooltip("Do NOT ship a real API key inside a client build. Use a server proxy for production.")]
    public string openAIApiKey = "";

    [Tooltip("Example: gpt-4.1-mini (change to an available model on your account).")]
    public string model = "gpt-4.1-mini";

    public float timeoutSeconds = 30f;

    private const string ProxyEndpoint  = "https://openai-proxy-lime.vercel.app/api/chat";
    private const string DirectEndpoint = "https://api.openai.com/v1/responses";

    // Editor + key set → call OpenAI directly (fast iteration, no proxy needed)
    // Editor + no key  → use proxy
    // WebGL build      → always use proxy
    private bool   UseProxy      => string.IsNullOrWhiteSpace(openAIApiKey);
    private string ActiveEndpoint => UseProxy ? ProxyEndpoint : DirectEndpoint;

    public IEnumerator Send(string systemPrompt, string userMessage, Action<string> onResult, Action<string> onError)
    {
#if UNITY_EDITOR
        if (!UseProxy && string.IsNullOrWhiteSpace(openAIApiKey))
        {
            onError?.Invoke("OpenAI API key is empty. Please set it on OpenAIClient.");
            yield break;
        }
#endif

        // Minimal Responses API JSON body
        string json =
            "{"
            + $"\"model\":\"{Escape(model)}\","
            + "\"input\":["
                + "{"
                    + "\"role\":\"system\","
                    + $"\"content\":[{{\"type\":\"input_text\",\"text\":\"{Escape(systemPrompt)}\"}}]"
                + "},"
                + "{"
                    + "\"role\":\"user\","
                    + $"\"content\":[{{\"type\":\"input_text\",\"text\":\"{Escape(userMessage)}\"}}]"
                + "}"
            + "]"
            + "}";

        using var req = new UnityWebRequest(ActiveEndpoint, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = Mathf.CeilToInt(timeoutSeconds);
        req.SetRequestHeader("Content-Type", "application/json");
        if (!UseProxy) req.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {req.responseCode} - {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        string raw = req.downloadHandler.text;

        try
        {
            var parsed = JsonUtility.FromJson<OpenAIResponsesApiResult>(raw);
            string text = ExtractFirstOutputText(parsed);
            onResult?.Invoke(string.IsNullOrEmpty(text) ? raw : text);
        }
        catch (Exception e)
        {
            onError?.Invoke("Parse error: " + e.Message + "\nRaw:\n" + raw);
        }
    }

    private static string ExtractFirstOutputText(OpenAIResponsesApiResult r)
    {
        if (r?.output == null) return null;

        foreach (var outItem in r.output)
        {
            if (outItem?.content == null) continue;
            foreach (var c in outItem.content)
            {
                if (c != null && c.type == "output_text") return c.text;
            }
        }
        return null;
    }

    /// <summary>
    /// Send with full conversation history.
    /// history: alternating user/assistant messages from oldest to newest (excludes current turn).
    /// </summary>
    public IEnumerator SendWithHistory(
        string systemPrompt,
        List<ChatMessage> history,
        string currentUserMessage,
        Action<string> onResult,
        Action<string> onError)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"model\":\"{Escape(model)}\",");
        sb.Append("\"input\":[");

        // System message
        sb.Append("{\"role\":\"system\",\"content\":[{\"type\":\"input_text\",\"text\":\"");
        sb.Append(Escape(systemPrompt));
        sb.Append("\"}]},");

        // History messages
        foreach (var msg in history)
        {
            string contentType = msg.role == "assistant" ? "output_text" : "input_text";
            sb.Append($"{{\"role\":\"{msg.role}\",\"content\":[{{\"type\":\"{contentType}\",\"text\":\"");
            sb.Append(Escape(msg.content));
            sb.Append("\"}]},");
        }

        // Current user message
        sb.Append("{\"role\":\"user\",\"content\":[{\"type\":\"input_text\",\"text\":\"");
        sb.Append(Escape(currentUserMessage));
        sb.Append("\"}]}");

        sb.Append("]}");
        string json = sb.ToString();

        using var req = new UnityWebRequest(ActiveEndpoint, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = Mathf.CeilToInt(timeoutSeconds);
        req.SetRequestHeader("Content-Type", "application/json");
        if (!UseProxy) req.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {req.responseCode} - {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        string raw = req.downloadHandler.text;
        try
        {
            var parsed = JsonUtility.FromJson<OpenAIResponsesApiResult>(raw);
            string text = ExtractFirstOutputText(parsed);
            onResult?.Invoke(string.IsNullOrEmpty(text) ? raw : text);
        }
        catch (Exception e)
        {
            onError?.Invoke("Parse error: " + e.Message + "\nRaw:\n" + raw);
        }
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length + 16);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': break; // drop CR
                case '\t': sb.Append("\\t");  break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                default:
                    // Escape other ASCII control characters
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
