using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueInputUI : MonoBehaviour
{
    public static DialogueInputUI Instance { get; private set; }
    public bool IsOpen => panel != null && panel.activeSelf;

    [Header("UI")]
    public GameObject    panel;
    public TMP_InputField inputField;
    public Button        submitButton;

    [Header("Refs")]
    public MentorNPC mentor;           // 拖拽MentorNPC
    public SimpleFPSController fps;    // 可选：输入时禁用移动
    public PlayerInteraction interaction; // 可选：输入时禁用拾取

    private bool _enterPressedThisFrame;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    void Start()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (inputField != null)
            inputField.onEndEdit.AddListener(_ => { if (_enterPressedThisFrame) Submit(); });
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        _enterPressedThisFrame = Keyboard.current.enterKey.wasPressedThisFrame ||
                                 Keyboard.current.numpadEnterKey.wasPressedThisFrame;

        if (panel == null || !panel.activeSelf) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
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