using UnityEngine;

public class CameraSway : MonoBehaviour
{
    [Header("Idle sway")]
    public float swayAmountX = 0.2f;
    public float swayAmountY = 0.5f;
    public float swaySpeed = 3f;

    [Header("References")]
    [SerializeField] Rigidbody rbPlayer;

    [Header("Descent-style visual roll")]
    [Tooltip("Max roll (degrees) driven by angular/yaw motion")]
    public float maxTurnRoll = 20f;
    [Tooltip("Max roll (degrees) driven by lateral/strafe motion")]
    public float maxStrafeRoll = 15f;
    [Tooltip("How snappy the camera corrects to the target roll (higher = snappier)")]
    public float rollSmoothing = 3f;
    [Tooltip("Lateral speed (units/s) that results in full strafe roll")]
    public float lateralSpeedForFullRoll = 20f;
    [Tooltip("Multiplier converting yaw rate (deg/s) into roll degrees")]
    public float angularToRollMultiplier = 0.44f;

    Vector3 initialPosition;
    Quaternion initialLocalRotation;
    float currentRoll = 0f;
    float rollVelocity = 0f; // for SmoothDampAngle

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        // positional sway
        if (rbPlayer != null)
        {
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmountX;
            float swayY = Mathf.Cos(Time.time * swaySpeed) * swayAmountY;
            transform.localPosition = initialPosition + new Vector3(swayX, swayY, 0f);
        }



        if (rbPlayer == null) return;

        // Convert world velocities into player-local space
        Vector3 localLinear = rbPlayer.transform.InverseTransformDirection(rbPlayer.linearVelocity);
        Vector3 localAngular = rbPlayer.transform.InverseTransformDirection(rbPlayer.angularVelocity); // rad/s

        // Yaw rate in degrees per second
        float yawDegPerSec = localAngular.y * Mathf.Rad2Deg;

        // Target roll from yaw (makes camera lean into turns)
        float targetFromYaw = -yawDegPerSec * angularToRollMultiplier;

        // Target roll from lateral movement (strafe/sliding)
        float lateralNorm = 0f;
        if (lateralSpeedForFullRoll > 1e-5f)
            lateralNorm = Mathf.Clamp(localLinear.x / lateralSpeedForFullRoll, -1f, 1f);
        float targetFromStrafe = -lateralNorm * maxStrafeRoll;

        // Combine and clamp
        float combinedTarget = Mathf.Clamp(targetFromYaw + targetFromStrafe, -(maxTurnRoll + maxStrafeRoll), (maxTurnRoll + maxStrafeRoll));

        // Smooth toward target roll. rollSmoothing controls snappiness.
        float smoothTime = 1f / Mathf.Max(0.0001f, rollSmoothing);
        currentRoll = Mathf.SmoothDampAngle(currentRoll, combinedTarget, ref rollVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        // Apply roll on top of initial rotation
        Quaternion rollQuat = Quaternion.Euler(0f, 0f, currentRoll);
        transform.localRotation = rollQuat * initialLocalRotation;
    }
}