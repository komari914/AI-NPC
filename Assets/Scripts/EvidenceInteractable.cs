using UnityEngine;

public class EvidenceInteractable : MonoBehaviour
{
    [Header("Evidence")]
    public string evidenceId = "EVIDENCE_001";
    public string evidenceDisplayName = "";   // Short name shown in journal directory

    [TextArea]
    public string evidenceDescription = "This is a piece of evidence.";

    [Header("Model Preview")]
    public GameObject previewModelPrefab; // 3D model shown in the inspection popup

    [Header("Visual Feedback")]
    public bool changeColorOnInspect = true;
    public Color inspectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Renderer targetRenderer;

    public bool HasBeenInspected => hasBeenInspected;

    private bool hasBeenInspected = false;
    private Color originalColor;
    private bool colorReady = false;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null && targetRenderer.material.HasProperty("_Color"))
        {
            originalColor = targetRenderer.material.color;
            colorReady = true;
        }
    }

    public void Interact()
    {
        if (hasBeenInspected) return;

        hasBeenInspected = true;

        Debug.Log("Evidence Inspected: " + evidenceDescription);

        if (changeColorOnInspect && targetRenderer != null && colorReady)
            targetRenderer.material.color = inspectedColor;

        // Show popup panel with evidence details
        if (EvidencePopupUI.Instance != null)
            EvidencePopupUI.Instance.Show(evidenceId, evidenceDescription, previewModelPrefab);

        // Notify progress manager
        if (CaseProgressManager.Instance != null)
            CaseProgressManager.Instance.OnEvidenceInspected(evidenceId, evidenceDescription);

        // Record evidence collection
        if (DataRecorder.Instance != null)
            DataRecorder.Instance.RecordEvidenceCollected(evidenceId, evidenceDescription);

        // Add to evidence journal
        EvidenceJournalUI journal = Object.FindFirstObjectByType<EvidenceJournalUI>();
        if (journal != null)
            journal.AddEntry(evidenceId, evidenceDescription, evidenceDisplayName);
    }
}