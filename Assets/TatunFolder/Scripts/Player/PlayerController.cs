using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // References
    StatManager statManager;
    Rigidbody rb;
    AudioSource engineSound;

    // Input bindings
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

    // Movement settings
    [Header("Translation")]
    public float maxForwardSpeed = 30f;
    public float maxStrafeSpeed = 30f;
    public float maxVerticalSpeed = 30f;
    public float acceleration = 100f;
    public float driftDecay = 40f;
    public float inputDeadzone = 0.1f;

    // Rotation settings
    [Header("Rotation")]
    public float yawSpeed = 120f;
    public float pitchSpeed = 120f;
    public float rollSpeed = 120f;
    public float angularAcceleration = 400f;
    public bool invertPitch = false;

    // Roll stabilization
    float rollInputLastTime = 0f;
    bool isStabilizingRoll = false;
    float rollStabilizeDelay = 0.2f;
    float rollStabilizeSpeed = 1.5f;

    // Dodge
    [Header("Dodge")]
    public float dodgeSpeed = 80f;
    public float dodgeCooldown = 1f;
    public float dodgeEnergyCost = 10f;
    public bool canDodge = true;

    // Collision tuning
    [Header("Collision tuning")]
    [Range(0f, 1f)] public float collisionTangentialRetention = 0.0f;
    [Range(0f, 1f)] public float collisionAngularRetention = 0.0f;
    public float collisionNormalIgnoreThreshold = 0.05f;

    // Unstuck and wall avoidance
    [Header("Unstuck")]
    public float stuckTimeThreshold = 0.3f;
    public float stuckProbeRadius = 0.6f;
    public float stuckVelocityThreshold = 0.6f;
    public float unstuckForce = 4f;
    public LayerMask unstuckObstacleMask = ~0;

    [Header("Wall Avoidance")]
    public bool enableWallAvoidance = true;
    public float wallAvoidDistance = 1.2f;
    public float wallAvoidStrength = 6f;
    public LayerMask wallAvoidMask = ~0;
    public bool respectPlayerInput = true;
    [Range(0f, 1f)] public float respectInputThreshold = 0.5f;
    [Header("Wall Avoidance Debug")] public bool drawWallAvoidGizmos = false;

    // VFX / audio
    [Header("Dodge VFX")]
    public ParticleSystem dodgeVfx;
    public Camera playerCamera;
    public float dodgeFovBoost = 12f;
    public float dodgeFovTime = 0.5f;

    [Header("Engine sound (pitch by speed)")]
    public float enginePitchMin = 0.3f;
    public float enginePitchMax = 1.0f;
    public float dodgePitch = 1.15f;
    public float enginePitchLerpSpeed = 6f;
    public float enginePitchReferenceSpeed = 0f;

    // runtime state
    Vector3 localVelocity;
    public bool wasTransInput = false;
    float stuckTimer = 0f;

    void Awake()
    {
        engineSound = GetComponent<AudioSource>();
        statManager = FindFirstObjectByType<StatManager>();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = 50f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            var wm = FindFirstObjectByType<WeaponManager>();
            if (playerCamera == null && wm != null) playerCamera = wm.aimCamera;
        }

        if (engineSound != null)
        {
            engineSound.loop = true;
            if (!engineSound.isPlaying) engineSound.Play();
            engineSound.pitch = enginePitchMin;
        }

        if (enginePitchReferenceSpeed <= 0f)
        {
            enginePitchReferenceSpeed = Mathf.Max(maxForwardSpeed, maxStrafeSpeed, maxVerticalSpeed);
            if (enginePitchReferenceSpeed <= 0f) enginePitchReferenceSpeed = 1f;
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

    private void Update()
    {
        UpdateEnginePitch();
    }

    void UpdateEnginePitch()
    {
        if (engineSound == null || rb == null) return;
        if (!canDodge) engineSound.pitch = dodgePitch;

        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / enginePitchReferenceSpeed);
        float targetPitch = Mathf.Lerp(enginePitchMin, enginePitchMax, t);
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * enginePitchLerpSpeed);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;

        if (Input.GetKeyDown(KeyCode.P))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        Vector2 leftStick = ReadVector2(translateAction);
        Vector2 rightStick = ReadVector2(rotateAction);
        float rollInput = ReadFloat(rollAction);
        float triggerUp = ReadFloat(upAction);
        float triggerDown = ReadFloat(downAction);

        if (Mathf.Abs(leftStick.x) < inputDeadzone) leftStick.x = 0f;
        if (Mathf.Abs(leftStick.y) < inputDeadzone) leftStick.y = 0f;
        if (Mathf.Abs(triggerUp) < inputDeadzone) triggerUp = 0f;
        if (Mathf.Abs(triggerDown) < inputDeadzone) triggerDown = 0f;

        float strafe = leftStick.x;
        float throttle = leftStick.y;
        float vertical = triggerUp - triggerDown;

        Vector3 targetLocalVelocity = new Vector3(strafe * maxStrafeSpeed, vertical * maxVerticalSpeed, throttle * maxForwardSpeed);
        bool hasTransInput = Mathf.Abs(strafe) > 1e-4f || Mathf.Abs(vertical) > 1e-4f || Mathf.Abs(throttle) > 1e-4f;

        if (hasTransInput)
        {
            localVelocity = Vector3.MoveTowards(localVelocity, targetLocalVelocity, acceleration * dt);
            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }
        else
        {
            Vector3 currentWorldVel = rb.linearVelocity;
            Vector3 decayedWorldVel = Vector3.MoveTowards(currentWorldVel, Vector3.zero, driftDecay * dt);
            rb.linearVelocity = decayedWorldVel;
            localVelocity = transform.InverseTransformDirection(decayedWorldVel);
        }

        wasTransInput = hasTransInput;

        float yawRate = rightStick.x * yawSpeed;
        float pitchRate = (invertPitch ? rightStick.y : -rightStick.y) * pitchSpeed;
        float rollRate = rollInput * rollSpeed;

        Vector3 targetLocalAngVelRad = new Vector3(pitchRate, yawRate, -rollRate) * Mathf.Deg2Rad;
        Vector3 targetWorldAngVel = transform.TransformDirection(targetLocalAngVelRad);
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

        if (isStabilizingRoll) StabilizeRoll(dt);

        CheckStuckAndUnstuck();
        ApplyWallAvoidance(dt, inputLocal: new Vector3(strafe, vertical, throttle));
    }

    void StabilizeRoll(float dt)
    {
        float currentRoll = transform.localEulerAngles.z;
        if (currentRoll > 180f) currentRoll -= 360f;
        float targetRoll = Mathf.Round(currentRoll / 90f) * 90f;
        float newRoll = Mathf.LerpAngle(currentRoll, targetRoll, rollStabilizeSpeed * dt);
        Vector3 euler = transform.localEulerAngles;
        euler.z = newRoll;
        transform.localEulerAngles = euler;
        if (Mathf.Abs(Mathf.DeltaAngle(newRoll, targetRoll)) < 0.5f) isStabilizingRoll = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;
        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal;

        Vector3 worldVel = rb.linearVelocity;
        Vector3 normalComponent = Vector3.Project(worldVel, normal);
        Vector3 tangential = worldVel - normalComponent;

        if (normalComponent.magnitude < collisionNormalIgnoreThreshold) normalComponent = Vector3.zero;

        Vector3 newTangential = tangential * Mathf.Clamp01(collisionTangentialRetention);
        rb.linearVelocity = newTangential;
        localVelocity = transform.InverseTransformDirection(newTangential);
        rb.angularVelocity = rb.angularVelocity * Mathf.Clamp01(collisionAngularRetention);
    }

    void CheckStuckAndUnstuck()
    {
        if (rb == null) return;
        if (rb.linearVelocity.magnitude < stuckVelocityThreshold)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, stuckProbeRadius, unstuckObstacleMask, QueryTriggerInteraction.Ignore);
            if (cols != null && cols.Length > 0)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > stuckTimeThreshold)
                {
                    Vector3 push = Vector3.zero;
                    foreach (var c in cols)
                    {
                        if (c == null) continue;
                        Vector3 closest = c.ClosestPoint(transform.position);
                        push += (transform.position - closest).normalized;
                    }
                    if (push.sqrMagnitude < 1e-6f) push = transform.forward;
                    push.Normalize();
                    rb.AddForce(push * unstuckForce, ForceMode.VelocityChange);
                    stuckTimer = 0f;
                }
                return;
            }
        }
        stuckTimer = 0f;
    }

    Vector3 ComputeWallAvoidanceVector()
    {
        if (!enableWallAvoidance || rb == null) return Vector3.zero;

        // Sample rays in multiple directions and accumulate surface normals weighted by proximity.
        Vector3[] sampleDirs = new Vector3[]
        {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized,
            (Vector3.up + Vector3.forward).normalized,
            (Vector3.up + Vector3.back).normalized,
            (Vector3.up + Vector3.right).normalized,
            (Vector3.up + Vector3.left).normalized,
            (Vector3.down + Vector3.forward).normalized,
            (Vector3.down + Vector3.back).normalized,
            (Vector3.down + Vector3.right).normalized,
            (Vector3.down + Vector3.left).normalized
        };

        Vector3 push = Vector3.zero;
        float maxRange = Mathf.Max(0.001f, wallAvoidDistance);

        for (int i = 0; i < sampleDirs.Length; i++)
        {
            Vector3 dirWorld = transform.TransformDirection(sampleDirs[i]);
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dirWorld, out hit, maxRange, wallAvoidMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;

                float weight = Mathf.Clamp01((maxRange - hit.distance) / maxRange);
                push += hit.normal * weight;
            }
        }

        if (push.sqrMagnitude < 1e-6f) return Vector3.zero;
        return push.normalized;
    }

    void ApplyWallAvoidance(float dt, Vector3 inputLocal)
    {
        if (!enableWallAvoidance || rb == null) return;
        Vector3 avoid = ComputeWallAvoidanceVector();
        if (avoid.sqrMagnitude < 1e-6f) return;

        float strength = wallAvoidStrength;
        if (respectPlayerInput && inputLocal.sqrMagnitude > 1e-4f)
        {
            Vector3 inputWorld = transform.TransformDirection(inputLocal.normalized);
            float dotIntoObstacle = Vector3.Dot(inputWorld, -avoid);
            if (dotIntoObstacle > respectInputThreshold) strength *= 0.25f;
        }

        rb.AddForce(avoid * strength, ForceMode.Acceleration);
        if (drawWallAvoidGizmos) Debug.DrawRay(transform.position, avoid * 1.5f, Color.cyan, 0.1f);
    }

    static Vector2 ReadVector2(InputActionProperty prop)
    {
        if (prop == null || prop.action == null) return Vector2.zero;
        var a = prop.action;
        try { return a.ReadValue<Vector2>(); }
        catch
        {
            try { Vector3 v3 = a.ReadValue<Vector3>(); return new Vector2(v3.x, v3.y); }
            catch { return Vector2.zero; }
        }
    }

    static float ReadFloat(InputActionProperty prop)
    {
        if (prop == null || prop.action == null) return 0f;
        try { return prop.action.ReadValue<float>(); } catch { return 0f; }
    }

    public void OnDodge() { PerformDodge(); }

    void PerformDodge()
    {
        if (StatManager.Instance != null && StatManager.Instance.energyCooldown) return;
        if (!canDodge || rb == null || statManager == null) return;

        Vector2 leftStick = ReadVector2(translateAction);
        float triggerUp = ReadFloat(upAction);
        float triggerDown = ReadFloat(downAction);
        float strafe = leftStick.x;
        float throttle = leftStick.y;
        float vertical = triggerUp - triggerDown;

        Vector3 inputLocal = new Vector3(strafe, vertical, throttle);
        if (inputLocal.sqrMagnitude <= 1e-4f) return;

        statManager.UseEnergy(dodgeEnergyCost);
        statManager.IsUsingEnergy = true;

        dodgeVfx?.Play();
        if (playerCamera != null) StartCoroutine(DoDodgeFov(playerCamera, dodgeFovBoost, dodgeFovTime));

        canDodge = false;
        StartCoroutine(DodgeCooldown());

        Vector3 dodgeDirLocal = inputLocal.normalized;
        Vector3 dodgeWorldDir = transform.TransformDirection(dodgeDirLocal).normalized;
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
        if (statManager != null) statManager.IsUsingEnergy = false;
    }
}
