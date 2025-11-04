using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
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

    [Header("Translation")]
    public float maxForwardSpeed = 80f;
    public float maxStrafeSpeed = 120f;
    public float maxVerticalSpeed = 120f;
    public float acceleration = 60f;
    public float driftDecay = 60f;
    public float inputDeadzone = 0.1f;

    [Header("Rotation")]
    public float yawSpeed = 120f;
    public float pitchSpeed = 120f;
    public float rollSpeed = 120f;
    public float angularAcceleration = 400f;
    public float rotationSmooth = 12f;
    public bool invertPitch = false;

    [Header("Collision tuning")]
    [Range(0f, 1f)]
    [Tooltip("Fraction of tangential velocity to keep after a collision. 0 = complete stop along surface, 1 = keep full tangent speed.")]
    public float collisionTangentialRetention = 0.2f;
    [Range(0f, 1f)]
    [Tooltip("Fraction of angular velocity to keep after a collision. 0 = stop rotating, 1 = keep full angular velocity.")]
    public float collisionAngularRetention = 0.1f;
    [Tooltip("If the normal (into-surface) component of velocity is small than this threshold, it's ignored.")]
    public float collisionNormalIgnoreThreshold = 0.05f;

    Rigidbody rb;
    Vector3 localVelocity;
    bool wasTransInput = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = 50f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnEnable()
    {
        translateAction.action?.Enable();
        rotateAction.action?.Enable();
        rollAction.action?.Enable();
        upAction.action?.Enable();
        downAction.action?.Enable();
    }

    void OnDisable()
    {
        translateAction.action?.Disable();
        rotateAction.action?.Disable();
        rollAction.action?.Disable();
        upAction.action?.Disable();
        downAction.action?.Disable();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;

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
            // Snap on initial input
            if (!wasTransInput)
            {
                localVelocity = targetLocalVelocity;
            }
            else
            {
                localVelocity = Vector3.MoveTowards(localVelocity, targetLocalVelocity, acceleration * dt);
            }

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
}