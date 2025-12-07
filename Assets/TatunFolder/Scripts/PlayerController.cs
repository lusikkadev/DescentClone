using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;



[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    StatManager statManager;


    [Header("Input (assign InputAction / PlayerInput -> Actions)")]
    [Tooltip("Left stick (Vector2): x = strafe (left/right), y = throttle (forward/back)")]
    public InputActionProperty translateAction;
    [Tooltip("Right stick (Vector2): x = yaw (turn left/right), y = pitch (up/down)")]
    public InputActionProperty rotateAction;
    [Tooltip("Shoulder buttons (1D axis composite): -1..1 for roll (left shoulder = -1, right shoulder = +1)")]
    public InputActionProperty rollAction;
    [Tooltip("Right trigger (Float 0..1): strafe up (vertical)")]
    public InputActionProperty upAction;
    [Tooltip("Left trigger (Float 0..1): strafe down (vertical)")]
    public InputActionProperty downAction;
    [Tooltip("Dodge / Boost action (Button)")]
    public InputActionReference dodgeAction;

    [Header("Translation")]
    public float maxForwardSpeed = 30f;
    public float maxStrafeSpeed = 30f;
    public float maxVerticalSpeed = 15f;
    public float acceleration = 100f;
    public float driftDecay = 40f;
    public float inputDeadzone = 0.1f;

    [Header("Rotation")]
    public float yawSpeed = 120f;
    public float pitchSpeed = 120f;
    public float rollSpeed = 120f;
    public float angularAcceleration = 400f;
    public float rotationSmooth = 12f;
    public bool invertPitch = false;
    // Roll stabilize
    float rollInputLastTime = 0f;
    bool isStabilizingRoll = false;
    float rollStabilizeDelay = 0.2f;
    float rollStabilizeSpeed = 1.5f;

    [Header("Dodge")]
    public float dodgeSpeed = 45f;
    public float dodgeCooldown = 1f;
    public float dodgeEnergyCost = 10f;
    public bool canDodge = true;

    [Header("Collision tuning")]
    [Range(0f, 1f)]
    [Tooltip("Fraction of tangential velocity to keep after a collision. 0 = complete stop along surface, 1 = keep full tangent speed.")]
    public float collisionTangentialRetention = 0.0f;
    [Range(0f, 1f)]
    [Tooltip("Fraction of angular velocity to keep after a collision. 0 = stop rotating, 1 = keep full angular velocity.")]
    public float collisionAngularRetention = 0.0f;
    [Tooltip("If the normal (into-surface) component of velocity is small than this threshold, it's ignored.")]
    public float collisionNormalIgnoreThreshold = 0.05f;

    // Dodge visuals
    [Header("Dodge VFX")]
    public ParticleSystem dodgeVfx;
    public Camera playerCamera;
    public float dodgeFovBoost = 12f;
    public float dodgeFovTime = 0.18f;

    Rigidbody rb;
    Vector3 localVelocity;
    public bool wasTransInput = false;

    void Awake()
    {
        statManager = FindFirstObjectByType<StatManager>();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = 50f;

        if (playerCamera == null)
        {
            // fallback to main camera or WeaponManager's camera
            playerCamera = Camera.main;
            var wm = FindFirstObjectByType<WeaponManager>();
            if (playerCamera == null && wm != null) playerCamera = wm.aimCamera;
        }
    }

    void OnEnable()
    {
        translateAction.action?.Enable();
        rotateAction.action?.Enable();
        rollAction.action?.Enable();
        upAction.action?.Enable();
        downAction.action?.Enable();

        dodgeAction?.action?.Enable();
    }

    void OnDisable()
    {
        translateAction.action?.Disable();
        rotateAction.action?.Disable();
        rollAction.action?.Disable();
        upAction.action?.Disable();
        downAction.action?.Disable();
        dodgeAction?.action?.Disable();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;

        if (Input.GetKeyDown(KeyCode.P))
        {
            //Reset scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // Read inputs
        Vector2 leftStick = ReadVector2(translateAction);
        Vector2 rightStick = ReadVector2(rotateAction);
        float rollInput = ReadFloat(rollAction);
        float triggerUp = ReadFloat(upAction);
        float triggerDown = ReadFloat(downAction);

        // Deadzones
        if (Mathf.Abs(leftStick.x) < inputDeadzone) leftStick.x = 0f;
        if (Mathf.Abs(leftStick.y) < inputDeadzone) leftStick.y = 0f;
        if (Mathf.Abs(triggerUp) < inputDeadzone) triggerUp = 0f;
        if (Mathf.Abs(triggerDown) < inputDeadzone) triggerDown = 0f;

        // Map controls
        float strafe = leftStick.x;
        float throttle = leftStick.y;
        float vertical = triggerUp - triggerDown;

        // Target local velocity (local-space)
        Vector3 targetLocalVelocity = new Vector3(
            strafe * maxStrafeSpeed,
            vertical * maxVerticalSpeed,
            throttle * maxForwardSpeed
        );

        bool hasTransInput = Mathf.Abs(strafe) > 1e-4f || Mathf.Abs(vertical) > 1e-4f || Mathf.Abs(throttle) > 1e-4f;


        if (hasTransInput)
        {
            localVelocity = Vector3.MoveTowards(localVelocity, targetLocalVelocity, acceleration * dt);

            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }
        else
        {
            // No input: driftii ja decay
            Vector3 currentWorldVel = rb.linearVelocity;
            Vector3 decayedWorldVel = Vector3.MoveTowards(currentWorldVel, Vector3.zero, driftDecay * dt);
            rb.linearVelocity = decayedWorldVel;

            // Keep local velocity for next input
            localVelocity = transform.InverseTransformDirection(decayedWorldVel);

        }

        wasTransInput = hasTransInput;


        // Rotation:
        float yawRate = rightStick.x * yawSpeed;
        float pitchRate = (invertPitch ? rightStick.y : -rightStick.y) * pitchSpeed;
        float rollRate = rollInput * rollSpeed;

        // target angular velocity in local space
        Vector3 targetLocalAngVelRad = new Vector3(pitchRate, yawRate, -rollRate) * Mathf.Deg2Rad;
        Vector3 targetWorldAngVel = transform.TransformDirection(targetLocalAngVelRad);

        // Smooth angular velocity change
        float angAccelRad = angularAcceleration * Mathf.Deg2Rad;
        rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, targetWorldAngVel, angAccelRad * dt);

        if (Mathf.Abs(rollInput) > inputDeadzone)
        {
            rollInputLastTime = Time.time;
            isStabilizingRoll = false;
        }
        else if (!isStabilizingRoll && Time.time - rollInputLastTime > rollStabilizeDelay)
        {
            isStabilizingRoll = true;
        }

        if (isStabilizingRoll)
        {
            StabilizeRoll(dt);
        }
    }

    void StabilizeRoll(float dt)
    {
        // get current local roll angle in degrees
        float currentRoll = transform.localEulerAngles.z;
        // normalize to -180..180
        if (currentRoll > 180f) currentRoll -= 360f;

        // 45 degree segments, find nearest
        float targetRoll = Mathf.Round(currentRoll / 90f) * 90f;

        // Smoothly rotate towards target roll
        float newRoll = Mathf.LerpAngle(currentRoll, targetRoll, rollStabilizeSpeed * dt);

        // Apply new roll
        Vector3 euler = transform.localEulerAngles;
        euler.z = newRoll;
        transform.localEulerAngles = euler;

        // Stop if close enough
        if (Mathf.Abs(Mathf.DeltaAngle(newRoll, targetRoll)) < 0.5f)
        {
            isStabilizingRoll = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Process hits and prevent going crazy

        if (collision.contactCount == 0) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal;

        Vector3 worldVel = rb.linearVelocity;
        Vector3 normalComponent = Vector3.Project(worldVel, normal);
        Vector3 tangential = worldVel - normalComponent;

        if (normalComponent.magnitude < collisionNormalIgnoreThreshold)
        {
            // Ignore small normal components. Jitter.
            normalComponent = Vector3.zero;
        }

        //damping tangential velocity
        Vector3 newTangential = tangential * Mathf.Clamp01(collisionTangentialRetention);

        // final velocity (tangential)
        rb.linearVelocity = newTangential;
        localVelocity = transform.InverseTransformDirection(newTangential);

        // damping angular velocity
        rb.angularVelocity = rb.angularVelocity * Mathf.Clamp01(collisionAngularRetention);

    }

    // Helpers
    static Vector2 ReadVector2(InputActionProperty prop)
    {
        if (prop == null || prop.action == null) return Vector2.zero;
        var a = prop.action;
        try
        {
            return a.ReadValue<Vector2>();
        }
        catch
        {
            try
            {
                Vector3 v3 = a.ReadValue<Vector3>();
                return new Vector2(v3.x, v3.y);
            }
            catch
            {
                return Vector2.zero;
            }
        }
    }

    static float ReadFloat(InputActionProperty prop)
    {
        if (prop == null || prop.action == null) return 0f;
        try { return prop.action.ReadValue<float>(); } catch { return 0f; }
    }

    // Dodge / Boost action towards move direction using dodgeMultiplier for speed

    public void OnDodge()
    {
        PerformDodge();
    }

    void PerformDodge()
    {
        // respect global energy lock
        if (StatManager.Instance != null && StatManager.Instance.energyCooldown) return;

        if (!canDodge || rb == null || statManager == null) return;

        // read current inputs (including vertical) and require a deliberate direction to dodge
        Vector2 leftStick = ReadVector2(translateAction);
        float triggerUp = ReadFloat(upAction);
        float triggerDown = ReadFloat(downAction);
        float strafe = leftStick.x;
        float throttle = leftStick.y;
        float vertical = triggerUp - triggerDown;

        Vector3 inputLocal = new Vector3(strafe, vertical, throttle);

        // require explicit input direction — abort if no direction is provided
        if (inputLocal.sqrMagnitude <= 1e-4f) return;

        // consume energy
        statManager.UseEnergy(dodgeEnergyCost);

        // mark using-energy to pause regen
        statManager.IsUsingEnergy = true;

        // visuals
        dodgeVfx?.Play();
        if (playerCamera != null)
            StartCoroutine(DoDodgeFov(playerCamera, dodgeFovBoost, dodgeFovTime));

        canDodge = false;
        StartCoroutine(DodgeCooldown());

        // compute dodge direction in world space using player orientation
        Vector3 dodgeDirLocal = inputLocal.normalized;
        Vector3 dodgeWorldDir = transform.TransformDirection(dodgeDirLocal).normalized;

        // apply fixed dodge speed
        Vector3 dodgeWorldVel = dodgeWorldDir * dodgeSpeed;

        rb.linearVelocity = dodgeWorldVel;
        localVelocity = transform.InverseTransformDirection(dodgeWorldVel);
    }

    IEnumerator DoDodgeFov(Camera cam, float boost, float duration)
    {
        if (cam == null) yield break;
        float startFov = cam.fieldOfView;
        float target = startFov + boost;
        float t = 0f;
        while (t < duration)
        {
            cam.fieldOfView = Mathf.Lerp(startFov, target, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        cam.fieldOfView = target;
        // return to normal quickly
        t = 0f;
        float retTime = duration * 0.6f;
        while (t < retTime)
        {
            cam.fieldOfView = Mathf.Lerp(target, startFov, t / retTime);
            t += Time.deltaTime;
            yield return null;
        }
        cam.fieldOfView = startFov;
    }

    IEnumerator DodgeCooldown()
    {
        yield return new WaitForSeconds(dodgeCooldown);
        canDodge = true;

        // end using-energy state after dodge cooldown
        if (statManager != null) statManager.IsUsingEnergy = false;
    }
}
