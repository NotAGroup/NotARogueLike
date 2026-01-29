using UnityEngine;

[DisallowMultipleComponent]
public class OpponentStats : MonoBehaviour
{
    public string className;

    [Header("Combat")]
    public float attackRate;
    public float stunDuration;

    public float swingDuration;
    public float strikeDuration;
    public float returnDuration;
    public float attackRange;
    public float attackDamage;

    [Header("Health")]
    public float maxHealth;
    public float healthRegRate;

    [Header("Movement")]
    public float wanderInterval;
    public float wanderRadius;
    public float rotationSpeed;
    public float movementSpeed;

    [Header("Perception")]
    public float memoryDuration;
    public float detectionRange;
    public float alertRange;

    [Header("Ranged Enemies")]
    public float aggressionDuration;
    public float aimDuration;

    public float avoidRadius;
    public float minDistanceToPlayer;

    public float shootDuration;

    public void ComputeFrom(OpponentClassDefinition def, int level) {
        className = def.className;

        attackRate = def[OpponentStatKey.AttackRate]?.ComputeFrom(level) ?? 1f;
		stunDuration = def[OpponentStatKey.StunDuration]?.ComputeFrom(level) ?? 1f;

		swingDuration = def[OpponentStatKey.SwingDuration]?.ComputeFrom(level) ?? 1f;
		strikeDuration = def[OpponentStatKey.StrikeDuration]?.ComputeFrom(level) ?? 1f;
		returnDuration = def[OpponentStatKey.ReturnDuration]?.ComputeFrom(level) ?? 1f;
		attackRange = def[OpponentStatKey.AttackRange]?.ComputeFrom(level) ?? 1f;
		attackDamage = def[OpponentStatKey.AttackDamage]?.ComputeFrom(level) ?? 1f;

		maxHealth = def[OpponentStatKey.MaxHealth]?.ComputeFrom(level) ?? 1f;
		healthRegRate = def[OpponentStatKey.HealthRegRate]?.ComputeFrom(level) ?? 1f;

		wanderInterval = def[OpponentStatKey.WanderInterval]?.ComputeFrom(level) ?? 1f;
		wanderRadius = def[OpponentStatKey.WanderRadius]?.ComputeFrom(level) ?? 1f;
		rotationSpeed = def[OpponentStatKey.RotationSpeed]?.ComputeFrom(level) ?? 1f;
		movementSpeed = def[OpponentStatKey.MovementSpeed]?.ComputeFrom(level) ?? 1f;

		memoryDuration = def[OpponentStatKey.MemoryDuration]?.ComputeFrom(level) ?? 1f;
		detectionRange = def[OpponentStatKey.DetectionRange]?.ComputeFrom(level) ?? 1f;

        aimDuration = def[OpponentStatKey.AimDuration]?.ComputeFrom(level) ?? 1f;

        alertRange = def[OpponentStatKey.AlertRange]?.ComputeFrom(level) ?? 1f;
        avoidRadius = def[OpponentStatKey.AvoidRadius]?.ComputeFrom(level) ?? 1f;
        minDistanceToPlayer = def[OpponentStatKey.MinDistanceToPlayer]?.ComputeFrom(level) ?? 1f;

        shootDuration = def[OpponentStatKey.ShootDuration]?.ComputeFrom(level) ?? 1f;
        aggressionDuration = def[OpponentStatKey.AggressionDuration]?.ComputeFrom(level) ?? 1f;
    }
}

public enum OpponentStatKey {
    StunDuration,
    SwingDuration,
    StrikeDuration,
    ReturnDuration,
    AttackRate,
    AttackRange,
    AttackDamage,
    MaxHealth,
    HealthRegRate,
    WanderInterval,
    WanderRadius,
    RotationSpeed,
    MovementSpeed,
    MemoryDuration,
    DetectionRange,

    // ranged opponents
    AimDuration,
    AlertRange,
    AvoidRadius,
    MinDistanceToPlayer,
    ShootDuration,
    AggressionDuration
};


