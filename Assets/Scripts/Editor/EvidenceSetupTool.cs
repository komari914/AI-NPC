using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool to create all evidence placeholder objects at once.
/// Menu: Tools > Detective Game > Create All Evidence Objects
/// </summary>
public class EvidenceSetupTool
{
    private struct EvidenceData
    {
        public string id;
        public string displayName;
        public string description;
        public Color gizmoColor;

        public EvidenceData(string id, string displayName, string description, Color color)
        {
            this.id          = id;
            this.displayName = displayName;
            this.description = description;
            this.gizmoColor  = color;
        }
    }

    private static readonly EvidenceData[] AllEvidence = new[]
    {
        // --- Alex's evidence (Red tint) ---
        new EvidenceData(
            "A1",
            "Marked Design Draft",
            "A printed subsystem design draft covered in handwritten annotations. " +
            "Sections are crossed out with notes such as 'this part was taken' and 'no credit — again.' " +
            "The handwriting matches Alex's known documents. " +
            "Establishes motive: Alex's original design ideas were incorporated into the final document by Daniel without attribution.",
            new Color(1f, 0.7f, 0.7f)
        ),
        new EvidenceData(
            "A2",
            "Taxi Ride Receipt",
            "A digital taxi receipt showing a departure from the office at 9:47 PM, " +
            "destination listed as a residential address. Alex claims this proves they left before the incident. " +
            "However, the fare shows NO 'Late Night Fee' surcharge — a charge automatically applied by the platform " +
            "to all rides dispatched after 9:00 PM. Compare with P2 to confirm the surcharge should be present. " +
            "This receipt appears to be fabricated.",
            new Color(1f, 0.7f, 0.7f)
        ),
        new EvidenceData(
            "A3",
            "Internal Wi-Fi Reconnection Log",
            "A network access log showing Alex's registered laptop reconnecting to the office Wi-Fi at 10:23 PM — " +
            "inside the medical examiner's time-of-death window (10:00 PM–11:00 PM). " +
            "Directly contradicts the taxi receipt (A2). " +
            "This is the key piece of physical evidence: Alex was back in the building during the murder.",
            new Color(1f, 0.4f, 0.4f)   // brighter = key clue
        ),

        // --- Project Manager's evidence (Blue tint) ---
        new EvidenceData(
            "P1",
            "Meeting Room Argument Record",
            "Handwritten notes from a heated internal planning meeting earlier that week. " +
            "Documents a serious dispute between Daniel and the Project Manager over budget and timeline. " +
            "One entry reads: 'unilateral decisions will not stand.' " +
            "Establishes PM motive but is not time-specific to the night of the incident.",
            new Color(0.7f, 0.7f, 1f)
        ),
        new EvidenceData(
            "P2",
            "Late-Night Taxi Receipt",
            "A digital taxi receipt showing the Project Manager departed from a nearby office complex at 9:14 PM. " +
            "The route and distance are comparable to Alex's receipt (A2). " +
            "Notably, the fare includes an itemised 'Late Night Fee' surcharge — " +
            "a standard charge automatically applied to all rides dispatched after 9:00 PM. " +
            "Alex's receipt (A2) covers a similar route at 9:47 PM yet shows NO such surcharge, exposing it as fabricated.",
            new Color(0.7f, 0.7f, 1f)
        ),
        new EvidenceData(
            "P3",
            "Remote Meeting Confirmation Email",
            "An automatically generated calendar confirmation and a video platform connection log. " +
            "Confirms the Project Manager joined a remote call with an external partner at 10:02 PM; " +
            "the call ended at 11:17 PM. " +
            "Provides a verified, timestamped alibi covering the entire time-of-death window. Eliminates the PM.",
            new Color(0.4f, 0.4f, 1f)   // key clue
        ),

        // --- Junior Programmer's evidence (Green tint) ---
        new EvidenceData(
            "J1",
            "Late Overtime Record",
            "A building access and HR overtime log. The Junior Programmer badged in at 8:30 PM " +
            "and remained clocked in past 11:00 PM. " +
            "Establishes that the JP was in the building during the window of death. " +
            "Does not by itself indicate proximity to Daniel's office — raises suspicion.",
            new Color(0.7f, 1f, 0.7f)
        ),
        new EvidenceData(
            "J2",
            "Task Submission Timestamp",
            "A version control commit log. Shows code submissions by the Junior Programmer " +
            "at 10:08 PM, 10:37 PM, and 10:59 PM, originating from the shared developer floor workstation. " +
            "The JP was actively committing code throughout the death window — " +
            "cross-reference with J3 to confirm why this makes murder impossible.",
            new Color(0.4f, 1f, 0.4f)   // key clue
        ),
        new EvidenceData(
            "J3",
            "Designer–JP Chat Log",
            "An internal messaging log between the Junior Programmer and a senior designer. " +
            "Shows that each of the JP's code submissions that night was made in direct response " +
            "to real-time requirement changes sent by the designer. " +
            "The response times are minutes apart — it is physically impossible for the JP " +
            "to have committed the murder and returned to active coding within these intervals. Eliminates JP.",
            new Color(0.4f, 1f, 0.4f)   // key clue
        ),
    };

    [MenuItem("Tools/Detective Game/Create All Evidence Objects")]
    static void CreateAllEvidence()
    {
        // Ensure the Evidence layer exists (warn if not)
        int evidenceLayer = LayerMask.NameToLayer("Evidence");
        if (evidenceLayer == -1)
        {
            Debug.LogWarning(
                "[EvidenceSetup] Layer 'Evidence' not found! " +
                "Please create it first: Edit > Project Settings > Tags and Layers. " +
                "Objects will be created on Default layer for now.");
            evidenceLayer = 0;
        }

        // Create a parent holder to keep Hierarchy tidy
        GameObject parent = new GameObject("--- Evidence Objects ---");
        Undo.RegisterCreatedObjectUndo(parent, "Create Evidence Objects");

        int index = 0;
        foreach (var data in AllEvidence)
        {
            GameObject obj = CreateEvidenceObject(data, evidenceLayer, index);
            obj.transform.SetParent(parent.transform, worldPositionStays: true);
            index++;
        }

        Selection.activeGameObject = parent;
        Debug.Log($"[EvidenceSetup] Created {AllEvidence.Length} evidence objects under '--- Evidence Objects ---'." +
                  " Move them to the correct positions in your scene.");
    }

    static GameObject CreateEvidenceObject(EvidenceData data, int layer, int index)
    {
        // Place them in a row so they're easy to find and move
        Vector3 pos = new Vector3(index * 2f, 0.5f, 0f);

        // Use a Cube as placeholder — swap with your actual prop later
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name        = $"Evidence_{data.id}_{data.displayName.Replace(" ", "_")}";
        obj.layer       = layer;
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(0.3f, 0.4f, 0.05f); // flat document shape

        // Color the placeholder so it's visually distinct in editor
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            // Create a unique material instance so colours don't share
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = data.gizmoColor;
            rend.sharedMaterial = mat;
        }

        // Add EvidenceInteractable
        EvidenceInteractable ei = obj.AddComponent<EvidenceInteractable>();
        ei.evidenceId           = data.id;
        ei.evidenceDisplayName  = data.displayName;
        ei.evidenceDescription  = data.description;
        ei.inspectedColor       = new Color(0.4f, 0.4f, 0.4f); // grey when inspected
        ei.changeColorOnInspect = true;

        Undo.RegisterCreatedObjectUndo(obj, $"Create Evidence {data.id}");

        Debug.Log($"[EvidenceSetup] Created {obj.name}");
        return obj;
    }

    [MenuItem("Tools/Detective Game/List Evidence IDs in Console")]
    static void ListEvidenceIDs()
    {
        Debug.Log("=== Evidence IDs ===");
        foreach (var e in AllEvidence)
            Debug.Log($"[{e.id}] {e.displayName}");
        Debug.Log("Key clues (trigger FinalQuestion): A3, P3, J2, J3");
    }
}
