using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EvidencePopupUI : MonoBehaviour
{
    public static EvidencePopupUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject      panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Button          closeButton;

    [Header("Model Preview")]
    public RawImage   modelPreviewImage;   // RawImage on the left side of the panel
    public Camera     previewCamera;       // Dedicated camera — culling mask: ModelPreview only
    public Transform  modelSpawnPoint;     // Empty Transform in front of the preview camera
    public float      dragRotateSpeed = 0.4f;

    [Header("Controls")]
    public SimpleFPSController fpsController;
    public PlayerInteraction   playerInteraction;

    public bool IsOpen => panel != null && panel.activeSelf;

    // ── Model preview state ─────────────────────────────────────────────────
    private GameObject    _previewModel;
    private RenderTexture _renderTexture;
    private bool          _isDragging;
    private Vector2       _lastMousePos;

    // Layer name must match a layer you create in Edit > Project Settings > Tags & Layers
    private const string PreviewLayerName = "ModelPreview";

    // ───────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel?.SetActive(false);
        previewCamera?.gameObject.SetActive(false);
    }

    void Start()
    {
        closeButton?.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!IsOpen) return;

        // Close on E / Escape
        if (Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            Close();
            return;
        }

        if (_previewModel == null) return;

        // ── Drag to rotate ────────────────────────────────────────────────
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Only start drag when cursor is over the preview image
                if (IsPointerOverRawImage(mousePos))
                {
                    _isDragging   = true;
                    _lastMousePos = mousePos;
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                _isDragging = false;

            if (_isDragging)
            {
                Vector2 delta = mousePos - _lastMousePos;
                _previewModel.transform.Rotate(Vector3.up,   -delta.x * dragRotateSpeed, Space.World);
                _previewModel.transform.Rotate(Vector3.right, delta.y * dragRotateSpeed, Space.World);
                _lastMousePos = mousePos;
                return; // skip auto-rotate while dragging
            }
        }

    }

    // ── Public API ──────────────────────────────────────────────────────────
    public void Show(string evidenceId, string description, GameObject previewPrefab = null)
    {
        if (panel == null) return;

        if (titleText != null) titleText.text = $"Evidence — {evidenceId}";
        if (bodyText  != null) bodyText.text  = description;

        panel.SetActive(true);

        if (fpsController    != null) fpsController.enabled    = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SpawnPreviewModel(previewPrefab);
    }

    public void Close()
    {
        panel?.SetActive(false);

        DestroyPreviewModel();

        if (fpsController    != null) fpsController.enabled    = true;
        if (playerInteraction != null) playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ── Model preview helpers ───────────────────────────────────────────────
    void SpawnPreviewModel(GameObject prefab)
    {
        DestroyPreviewModel();

        bool hasPreview = prefab != null && previewCamera != null &&
                          modelPreviewImage != null && modelSpawnPoint != null;

        if (modelPreviewImage != null)
            modelPreviewImage.gameObject.SetActive(hasPreview);

        if (!hasPreview) return;

        // Create (or reuse) render texture
        _renderTexture ??= new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { antiAliasing = 4 };

        previewCamera.targetTexture = _renderTexture;
        modelPreviewImage.texture   = _renderTexture;

        // Spawn model and move to preview layer so only preview camera sees it
        _previewModel = Instantiate(prefab, modelSpawnPoint.position, modelSpawnPoint.rotation);
        SetLayerRecursive(_previewModel, LayerMask.NameToLayer(PreviewLayerName));

        previewCamera.gameObject.SetActive(true);
        _isDragging = false;
    }

    void DestroyPreviewModel()
    {
        if (_previewModel != null) { Destroy(_previewModel); _previewModel = null; }
        previewCamera?.gameObject.SetActive(false);
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (layer < 0) return; // layer not found — skip silently
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    bool IsPointerOverRawImage(Vector2 screenPos)
    {
        if (modelPreviewImage == null) return false;
        RectTransform rt = modelPreviewImage.rectTransform;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos);
    }
}
