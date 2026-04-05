using UnityEngine;

/// <summary>
/// Manages the persistent in-game HUD:
///   - Crosshair dot at screen centre
///   - Top-left key hints (TAB / H)
///
/// Attach to a always-active parent GameObject.
/// Wire up the three RectTransform/GameObject references in the Inspector.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public GameObject crosshair;      // Image (dot) anchored to centre
    public GameObject keyHints;       // TextMeshPro or panel anchored to top-left

    void Update()
    {
        // Hide crosshair and hints whenever the cursor is unlocked
        // (evidence popup, journal, pause menu, controls panel all unlock the cursor)
        bool gameplayActive = Cursor.lockState == CursorLockMode.Locked;

        if (crosshair != null) crosshair.SetActive(gameplayActive);
        if (keyHints  != null) keyHints.SetActive(gameplayActive);
    }
}
