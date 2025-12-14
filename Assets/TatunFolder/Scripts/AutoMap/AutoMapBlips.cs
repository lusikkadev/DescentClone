using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AutomapController))]
public class AutoMapBlips : MonoBehaviour
{
    [Header("References")]
    public Camera mapCamera;
    public RawImage mapDisplay;

    [Header("Player Blip")]
    public RectTransform playerBlip;

    [Header("Enemy Blips (optional)")]
    public GameObject enemyBlipPrefab;
    public string enemyTag = "Enemy";
    public bool showEnemies = false;

    // internal
    AutomapController automap;
    public Transform player;
    Camera uiCamera; // null for Overlay canvases
    Dictionary<Transform, RectTransform> enemyBlips = new Dictionary<Transform, RectTransform>();

    void Awake()
    {
        automap = GetComponent<AutomapController>();
        if (mapCamera == null && automap != null) mapCamera = automap.mapCamera;
        if (mapDisplay == null && automap != null) mapDisplay = automap.mapDisplay;
    }

    void Start()
    {
        Canvas c = mapDisplay != null ? mapDisplay.canvas : null;
        uiCamera = (c != null && c.renderMode == RenderMode.ScreenSpaceCamera) ? c.worldCamera : null;

        // If enemies wanted to be shown, here come the blips

        if (showEnemies && enemyBlipPrefab != null)
            RefreshEnemyBlips();
    }

    void OnEnable()
    {
        SetBlipsActive(mapDisplay != null && mapDisplay.gameObject.activeInHierarchy);
    }

    void Update()
    {
        if (mapDisplay == null || mapCamera == null || !mapDisplay.gameObject.activeInHierarchy) return;

        UpdatePlayerBlip();
        if (showEnemies) UpdateEnemyBlips();
    }

    bool ViewportToMapLocalPoint(Vector3 viewportPos, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        RectTransform rt = mapDisplay.rectTransform;
        if (rt == null) return false;

        if (viewportPos.z < 0f) return false;

        Vector2 rectSize = rt.rect.size;
        Vector2 centered = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
        Vector2 pivotOffset = new Vector2((0.5f - rt.pivot.x) * rectSize.x, (0.5f - rt.pivot.y) * rectSize.y);

        localPoint = new Vector2(centered.x * rectSize.x, centered.y * rectSize.y) + pivotOffset;

        Vector2 half = rectSize * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x, -half.x + 1f, half.x - 1f);
        localPoint.y = Mathf.Clamp(localPoint.y, -half.y + 1f, half.y - 1f);

        return true;
    }

    void UpdatePlayerBlip()
    {
        if (player == null || playerBlip == null) { if (playerBlip != null) playerBlip.gameObject.SetActive(false); return; }

        Vector3 worldPos = player.position;
        Vector3 vp = mapCamera.WorldToViewportPoint(worldPos);

        if (vp.z < 0f)
        {
            playerBlip.gameObject.SetActive(false);
            return;
        }

        if (ViewportToMapLocalPoint(vp, out Vector2 localPoint))
        {
            playerBlip.anchoredPosition = localPoint;
            playerBlip.gameObject.SetActive(true);
        }
        else
        {
            playerBlip.gameObject.SetActive(false);
        }
    }

    void RefreshEnemyBlips()
    {
        foreach (var kv in enemyBlips) if (kv.Value != null) Destroy(kv.Value.gameObject);
        enemyBlips.Clear();

        var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (var e in enemies)
        {
            var rt = Instantiate(enemyBlipPrefab, mapDisplay.transform).GetComponent<RectTransform>();
            rt.gameObject.SetActive(false);
            enemyBlips[e.transform] = rt;
        }
    }

    void UpdateEnemyBlips()
    {
        var found = GameObject.FindGameObjectsWithTag(enemyTag);
        if (found.Length != enemyBlips.Count) RefreshEnemyBlips();

        foreach (var kv in enemyBlips)
        {
            var t = kv.Key;
            var rt = kv.Value;
            if (t == null || rt == null) { if (rt != null) rt.gameObject.SetActive(false); continue; }

            Vector3 vp = mapCamera.WorldToViewportPoint(t.position);
            if (vp.z < 0f) { rt.gameObject.SetActive(false); continue; }

            if (ViewportToMapLocalPoint(vp, out Vector2 localPoint))
            {
                rt.anchoredPosition = localPoint;
                rt.gameObject.SetActive(true);
            }
            else
            {
                rt.gameObject.SetActive(false);
            }
        }
    }

    void SetBlipsActive(bool active)
    {
        if (playerBlip != null) playerBlip.gameObject.SetActive(active);
        foreach (var kv in enemyBlips) if (kv.Value != null) kv.Value.gameObject.SetActive(active);
    }
    public void OnMapOpened() => SetBlipsActive(true);
    public void OnMapClosed() => SetBlipsActive(false);
}