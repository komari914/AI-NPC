using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInputUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;           // 一个Panel，包含输入框
    public TMP_InputField inputField;  // TMP_InputField

    [Header("Refs")]
    public MentorNPC mentor;           // 拖拽MentorNPC
    public SimpleFPSController fps;    // 可选：输入时禁用移动
    public PlayerInteraction interaction; // 可选：输入时禁用拾取

    void Awake()
    {
        Hide();
    }

    void Update()
    {
        if (panel == null) return;

        // 回车提交
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Submit();
        }

        // Esc 关闭
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    public void Show()
    {
        panel.SetActive(true);

        if (fps != null) fps.enabled = false;
        if (interaction != null) interaction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
    }

    public void Hide()
    {
        panel.SetActive(false);

        if (fps != null) fps.enabled = true;
        if (interaction != null) interaction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Submit()
    {
        if (mentor == null || inputField == null) return;

        string msg = (inputField.text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(msg)) return;

        Hide();
        mentor.TalkWithPlayerInput(msg); // ✅ 交给Mentor处理
    }
}