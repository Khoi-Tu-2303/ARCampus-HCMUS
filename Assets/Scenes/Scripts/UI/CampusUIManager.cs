// UI/CampusUIManager.cs — PATCHED
// FIXES:
// [HIGH] Singleton Awake() had no duplicate check — if scene reloads, old Instance
//        becomes a dangling reference. Added standard guard pattern.

using UnityEngine;

public class CampusUIManager : MonoBehaviour
{
    public static CampusUIManager Instance;

    [Header("UI Chính")]
    public GameObject bottomBar;

    [Header("Màn hình Popup (Overlays)")]
    public GameObject searchOverlay;
    public GameObject mapOverlay;
    public GameObject navigationOverlay;

    void Awake()
    {
        // Safe singleton: destroy duplicate, never leave dangling reference
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CloseAllPanels();
    }

    // ──────────────────────────────────────────────────────────
    // BOTTOM BAR NAVIGATION
    // ──────────────────────────────────────────────────────────

    public void ToggleSearch()
    {
        bool isCurrentlyOn = searchOverlay.activeSelf;
        searchOverlay.SetActive(!isCurrentlyOn);
        if (!isCurrentlyOn && mapOverlay != null) mapOverlay.SetActive(false);
    }

    public void ToggleMap()
    {
        bool isCurrentlyOn = mapOverlay.activeSelf;
        mapOverlay.SetActive(!isCurrentlyOn);
        if (!isCurrentlyOn && searchOverlay != null) searchOverlay.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    // PANEL MANAGEMENT
    // ──────────────────────────────────────────────────────────

    public void CloseAllPanels()
    {
        if (searchOverlay != null) searchOverlay.SetActive(false);
        if (mapOverlay != null) mapOverlay.SetActive(false);
        if (navigationOverlay != null) navigationOverlay.SetActive(false);
        if (bottomBar != null) bottomBar.SetActive(true);
    }

    public void StartNavigation()
    {
        CloseAllPanels();
        if (bottomBar != null) bottomBar.SetActive(true);
        if (navigationOverlay != null) navigationOverlay.SetActive(true);
    }

    public void StopNavigation()
    {
        if (navigationOverlay != null) navigationOverlay.SetActive(false);
        if (bottomBar != null) bottomBar.SetActive(true);
    }
}