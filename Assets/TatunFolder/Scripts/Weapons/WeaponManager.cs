using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera; // assign the main camera (child camera)
    public Rigidbody ownerRb; // assign player's rigidbody
    public List<WeaponBase> weapons = new List<WeaponBase>();
    int currentIndex = 0;

    [Header("Input (optional)")]
    [Tooltip("If true WeaponManager will enable/handle the provided fireAction. Otherwise call FireCurrent() from your input code.")]
    public bool handleInput = true;
    public InputActionReference fireAction;
    public bool fireHoldToAuto = true;
    public float fireHoldThreshold = 0.5f;

    [Header("Aim smoothing (stabilize CameraSway)")]
    [Tooltip("0 = no smoothing. Higher values smooth more quickly (recommended 4..12).")]
    public float aimSmoothing = 8f;

    // internal smoothing state
    Vector3 smoothedAimDir = Vector3.forward;
    Ray lastSmoothedAimRay = new Ray(Vector3.zero, Vector3.forward);

    void Start()
    {
        if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        if (ownerRb == null) ownerRb = GetComponent<Rigidbody>();

        foreach (var w in weapons)
        {
            if (w != null) w.Initialize(aimCamera, ownerRb);
            w?.gameObject.SetActive(false);
        }
        if (weapons.Count > 0 && weapons[0] != null)
        {
            weapons[0].gameObject.SetActive(true);
            weapons[0].OnEquip();
        }

        // init smoothing direction from camera if available
        if (aimCamera != null)
        {
            smoothedAimDir = aimCamera.transform.forward.normalized;
            lastSmoothedAimRay = new Ray(aimCamera.transform.position, smoothedAimDir);
        }
    }

    void OnEnable()
    {
        if (handleInput && fireAction?.action != null)
        {
            fireAction.action.Enable();
            if (!fireHoldToAuto)
            {
                // avoid duplicate subscriptions if OnEnable is called multiple times
                fireAction.action.performed -= OnFirePerformed;
                fireAction.action.performed += OnFirePerformed;
            }
        }
    }

    void OnDisable()
    {
        if (handleInput && fireAction?.action != null)
        {
            if (!fireHoldToAuto)
                fireAction.action.performed -= OnFirePerformed;
            fireAction.action.Disable();
        }
    }

    void Update()
    {
        // update aim ray (smoothed) every frame so input callbacks can use latest
        if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        if (aimCamera == null) return;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray raw = aimCamera.ScreenPointToRay(center);
        Vector3 rawDir = raw.direction.normalized;

        if (smoothedAimDir == Vector3.zero) smoothedAimDir = rawDir;

        if (aimSmoothing <= 0f)
        {
            smoothedAimDir = rawDir;
        }
        else
        {
            float t = 1f - Mathf.Exp(-aimSmoothing * Time.deltaTime); // exponential smoothing factor
            smoothedAimDir = Vector3.Slerp(smoothedAimDir, rawDir, t).normalized;
        }

        lastSmoothedAimRay = new Ray(raw.origin, smoothedAimDir);

        // input handling (poll when holding)
        if (handleInput && fireHoldToAuto && fireAction?.action != null)
        {
            
            float v = ReadFloat(fireAction);
            if (v >= fireHoldThreshold)
            {
                Debug.Log("player fired the gun");
                FireCurrent(lastSmoothedAimRay);
            }
        }
    }

    // called by input performed (single-press)
    void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("player fired the weapon");
        FireCurrent(lastSmoothedAimRay);
    }

    public void SwitchTo(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        weapons[currentIndex]?.OnUnequip();
        weapons[currentIndex]?.gameObject.SetActive(false);
        currentIndex = index;
        weapons[currentIndex]?.gameObject.SetActive(true);
        weapons[currentIndex]?.OnEquip();
    }

    // legacy/compat wrapper - uses last computed smoothed ray
    public void FireCurrent()
    {
        FireCurrent(lastSmoothedAimRay);
    }

    // main entry: fire using the provided aim ray (typically smoothed)
    public void FireCurrent(Ray aimRay)
    {
        if (weapons.Count == 0) return;
        weapons[currentIndex]?.Fire(aimRay);
    }

    static float ReadFloat(InputActionReference prop)
    {
        if (prop == null || prop.action == null) return 0f;
        try { return prop.action.ReadValue<float>(); } catch { return 0f; }
    }
}