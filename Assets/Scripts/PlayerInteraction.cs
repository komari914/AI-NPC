using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask evidenceLayer;
    public Transform playerCamera;

    [Header("NPC")]
    public MentorNPC mentorNPC;
    public float npcTalkDistance = 3f;

    [Header("UI")]
    public UIHintController hintUI;

    [Header("Scenario (for hint text)")]
    public ScenarioManager scenarioManager;

    private EvidenceInteractable currentEvidence;

    void Update()
    {
        UpdateFocusedEvidence();
        HandleInteract();
    }

    void UpdateFocusedEvidence()
    {
        currentEvidence = null;

        if (playerCamera == null) return;

        Debug.DrawRay(playerCamera.position, playerCamera.forward * interactDistance, Color.red);

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, evidenceLayer))
            currentEvidence = hit.collider.GetComponentInParent<EvidenceInteractable>();

        if (hintUI == null) return;

        // Priority 1: uninspected evidence in crosshair
        if (currentEvidence != null && !currentEvidence.HasBeenInspected)
        {
            hintUI.Show("[E]  Inspect");
            return;
        }

        // Priority 2: near NPC
        if (mentorNPC != null)
        {
            float dist = Vector3.Distance(playerCamera.position, mentorNPC.transform.position);
            if (dist <= npcTalkDistance)
            {
                bool isVoice = scenarioManager != null &&
                               scenarioManager.modality == ModalityType.Voice;
                hintUI.Show(isVoice ? "[V]  Speak to Mentor" : "[F]  Talk to Mentor");
                return;
            }
        }

        hintUI.Hide();
    }

    void HandleInteract()
    {
        if (currentEvidence == null) return;
        if (EvidencePopupUI.Instance != null && EvidencePopupUI.Instance.IsOpen) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentEvidence.Interact();
    }
}