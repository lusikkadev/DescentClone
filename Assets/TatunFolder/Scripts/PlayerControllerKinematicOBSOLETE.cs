using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerKinematic : MonoBehaviour
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
    public float maxForwardSpeed = 30f;
    public float maxStrafeSpeed = 30f;
    public float maxVerticalSpeed = 30f;
    public float acceleration = 30f;    // how fast localVelocity moves toward target while input active (units/s^2)
    public float driftDecay = 25f;      // how fast current velocity decays while no input (units/s^2)
    public float inputDeadzone = 0.1f;

    [Header("Rotation")]
    public float yawSpeed = 200f;       // deg/s
    public float pitchSpeed = 200f;     // deg/s
    public float rollSpeed = 200f;      // deg/s
    public float angularAcceleration = 720f; // deg/s^2 - how fast angular velocity approaches target
    public float rotationSmooth = 12f;  // used only if you prefer Slerp approach for rotation
    public bool invertPitch = false;

    Rigidbody rb;
    Vector3 localVelocity; // local-space target/working velocity (m/s)
    bool wasTransInput = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = 50f;

        // Make the body dynamic so the physics solver blocks movement and rotation properly
        rb.isKinematic = false;
        // Use continuous collision detection for better behavior at higher speeds
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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

        // Update localVelocity and apply to Rigidbody.velocity (world-space)
        if (hasTransInput)
        {
            // Snap on initial input so previous drift doesn't interfere with new direction
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
            // No input: let the physics velocity drift but apply manual decay so ship slows smoothly
            Vector3 currentWorldVel = rb.linearVelocity;
            Vector3 decayedWorldVel = Vector3.MoveTowards(currentWorldVel, Vector3.zero, driftDecay * dt);
            rb.linearVelocity = decayedWorldVel;

            // Keep localVelocity in sync so when input resumes we snap cleanly
            localVelocity = transform.InverseTransformDirection(decayedWorldVel);
        }

        wasTransInput = hasTransInput;

        // Rotation: compute target local angular velocity in rad/s, map to world and approach it
        float yawRate = rightStick.x * yawSpeed;
        float pitchRate = (invertPitch ? rightStick.y : -rightStick.y) * pitchSpeed;
        float rollRate = rollInput * rollSpeed;

        // target angular velocity in local space (degrees -> radians)
        Vector3 targetLocalAngVelRad = new Vector3(pitchRate, yawRate, -rollRate) * Mathf.Deg2Rad;
        Vector3 targetWorldAngVel = transform.TransformDirection(targetLocalAngVelRad);

        // Smooth angular velocity change (convert angularAcceleration to rad/s^2)
        float angAccelRad = angularAcceleration * Mathf.Deg2Rad;
        rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, targetWorldAngVel, angAccelRad * dt);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Keep localVelocity consistent with physics result after a collision (helpful when we snap on next input)
        // Project velocity onto tangent plane so we don't keep a component into the collider normal.
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            Vector3 worldVel = rb.linearVelocity;
            Vector3 tangential = Vector3.ProjectOnPlane(worldVel, contact.normal);
            rb.linearVelocity = tangential;
            localVelocity = transform.InverseTransformDirection(tangential);
        }
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