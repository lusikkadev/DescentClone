using UnityEngine;

[CreateAssetMenu(fileName = "EnemyProfile", menuName = "AI/EnemyProfile", order = 100)]
public class EnemyProfileSO : ScriptableObject
{
    [Header("Movement")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 8f;
    public float maxAccel = 40f;
    public float linearDamping = 0.92f;

    [Header("Avoidance")]
    public float avoidDistance = 3f;
    public int avoidRays = 7;
    public float avoidStrength = 8f;

    [Header("Combat")]
    public float desiredCombatDistance = 10f;
    public float fireInterval = 1.5f;
    public float projectileSpeed = 40f;
    public float detectionRange = 40f;
    [Range(0f, 180f)] public float detectionAngle = 90f;
    public float loseSightTime = 3.0f;

    [Tooltip("Maximum cone angle in degrees for firing inaccuracy. Set to 0 for perfectly forward shots.")]
    public float aimSpreadDegrees = 6f;
    [Tooltip("0 = fire straight from muzzle, 1 = fully lead shots using target velocity/ projectile speed.")]
    [Range(0f, 1f)] public float aimPredictionFactor = 0.6f;

    [Header("Evasion")]
    public float evadeAmplitude = 0.6f;
    public float evadeFrequency = 1.2f;

    [Header("Orbiting")]
    public bool enableOrbit = false;
    public float orbitSpeed = 2.0f;
    public float orbitRadius = 0.6f;
    public bool orbitClockwise = false;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    [Header("Alarm")]
    public float alarmRange = 18f;
    public bool alarmOthers = true;

    [Header("Stuck / Unstuck")]
    public float stuckTimeThreshold = 0.6f;
    public float stuckProbeRadius = 0.6f;
    public float stuckVelocityThreshold = 0.6f;
    public float unstuckForce = 6f;

    [Header("Misc")]
    public bool drawDebugGizmos = false;
    public int health = 50;
}