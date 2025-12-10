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

    [Header("Evasion")]
    public float evadeAmplitude = 0.6f;
    public float evadeFrequency = 1.2f;

    [Header("Rotation")]
    public float rotationSpeed = 8f;
}