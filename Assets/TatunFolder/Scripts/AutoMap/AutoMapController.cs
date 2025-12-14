using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class AutomapController : MonoBehaviour
{
    public Camera mapCamera;

    public RawImage mapDisplay;

    public InputActionReference toggleInputAction;

    public float autoCloseSeconds = 0f;

    bool isOpen = false;
    float openTime;

    void Start()
    {
        if (mapCamera != null) mapCamera.enabled = false;
        if (mapDisplay != null) mapDisplay.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (toggleInputAction != null && toggleInputAction.action != null)
        {
            toggleInputAction.action.performed += OnToggleActionPerformed;
            toggleInputAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (toggleInputAction != null && toggleInputAction.action != null)
        {
            toggleInputAction.action.performed -= OnToggleActionPerformed;
            toggleInputAction.action.Disable();
        }
    }

    void Update()
    {

        if (isOpen && autoCloseSeconds > 0f && Time.time - openTime >= autoCloseSeconds)
        {
            CloseMap();
        }
    }

    void OnToggleActionPerformed(InputAction.CallbackContext ctx)
    {
        ToggleMap();
    }

    public void ToggleMap()
    {
        if (isOpen) CloseMap(); else OpenMap();
    }

    public void OpenMap()
    {
        if (mapCamera != null) mapCamera.enabled = true;
        if (mapDisplay != null) mapDisplay.gameObject.SetActive(true);
        isOpen = true;
        openTime = Time.time;
    }

    public void CloseMap()
    {
        if (mapCamera != null) mapCamera.enabled = false;
        if (mapDisplay != null) mapDisplay.gameObject.SetActive(false);
        isOpen = false;
    }
}