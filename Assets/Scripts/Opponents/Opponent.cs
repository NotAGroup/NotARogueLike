using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(OpponentStats))]
public abstract class Opponent : MonoBehaviour
{
    public enum OpponentState
    {
        Combat,
        Dead,
        Idle,
        Stunned
    }

    public struct OptionalVector3
    {
        public bool hasValue;
        public Vector3 value;
    }

    // Components
    protected Animator animator;
    protected NavMeshAgent navMeshAgent;
    protected Transform playerTransform;
    protected Player player;
    protected OpponentState state;
    protected OpponentStats stats;

    protected float currentHealth;

    protected bool attacking, wandering = false;
    protected float attackCooldown, stunCooldown;
    protected float aggressionModifier = 1.0f;
    protected float aggressionTimer, memoryTimer, wanderTimer;

    public Node spawnRoom;
    protected Vector3 spawnRoomCenter;
    protected int nextNavPointID = 0;
    protected List<NavPoint> navPoints;

    protected OptionalVector3 damageDirection;
    protected Vector3 direction, velocity;
    protected Quaternion rotation;

    public bool playerGotHit = false;

    public float viewAngle;

    protected virtual void Start()
    {
        stats = GetComponent<OpponentStats>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = stats.movementSpeed;

        animator = GetComponentInChildren<Animator>();

        playerTransform = GameObject.Find("Player").transform;
        player = playerTransform?.GetComponent<Player>();

        currentHealth = stats.maxHealth;

        aggressionTimer = 0.0f;
        wanderTimer = stats.wanderInterval;
    }

    protected virtual void Update()
    {
        if (state == OpponentState.Dead || playerTransform == null)
        {
            return;
        }

        switch (state)
        {
            case OpponentState.Combat:
                Combat();
                break;

            case OpponentState.Idle:
                Idle();
                break;

            case OpponentState.Stunned:
                Stunned();
                break;
        }

        velocity = transform.InverseTransformDirection(navMeshAgent.velocity);
        UpdateMovementAnimation();

        if (attackCooldown > 0.0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0.0f)
            {
                attackCooldown = 0.0f;
            }
        }
    }

    protected abstract void Attack();

    protected abstract void Combat();

    protected abstract bool CanSeePlayer();

    protected virtual void Stunned()
    {
        if (damageDirection.hasValue)
        {
            navMeshAgent.isStopped = true;

            velocity = Vector3.zero;
            UpdateMovementAnimation();

            if (Vector3.Angle(transform.forward, damageDirection.value) > 1.0f)
            {
                RotateTowards(damageDirection.value);
                return;
            }
            else
            {
                damageDirection.hasValue = false;
            }
        }

        stunCooldown -= Time.deltaTime;

        if (stunCooldown <= 0.0f)
        {
            stunCooldown = 0.0f;

            navMeshAgent.isStopped = false;

            if (CanSeePlayer() && !player.isDead)
            {
                memoryTimer = stats.memoryDuration;
                state = OpponentState.Combat;

                float distance = Vector3.Distance(playerTransform.position, transform.position);

                if (attackCooldown == 0.0f && distance <= stats.attackRange * aggressionModifier)
                {
                    Attack();
                }
            }
            else
            {
                state = OpponentState.Idle;
            }
        }
    }

    protected virtual void Die()
    {
        navMeshAgent.isStopped = true;

        animator.SetBool("isDead", true);
        state = OpponentState.Dead;

        Destroy(gameObject, 2.0f);
    }

    protected abstract void Idle();

    protected virtual void UpdateMovementAnimation()
    {
        animator.SetFloat("MoveX", velocity.x, 0.25f, Time.deltaTime);
        animator.SetFloat("MoveZ", velocity.z, 0.25f, Time.deltaTime);
    }

    protected virtual void RegenerateHealth()
    {
        if (currentHealth < stats.maxHealth)
        {
            float health = currentHealth + stats.healthRegRate * Time.deltaTime;
            currentHealth = Mathf.Min(health, stats.maxHealth);
        }
    }

    protected virtual void RotateTowards(Vector3 direction)
    {
        rotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rotation,
                Time.deltaTime * stats.rotationSpeed
            );
    }

    public virtual void TakeDamage(float damage, Vector3? direction = null)
    {
        navMeshAgent.isStopped = true;
        animator.SetTrigger("gotHit");

        if (direction.HasValue)
        {
            damageDirection.hasValue = true;
            damageDirection.value = direction.Value;
        }
        else
        {
            damageDirection.hasValue = false;
            damageDirection.value = Vector3.zero;
        }

        state = OpponentState.Stunned;
        stunCooldown = stats.stunDuration;
        currentHealth -= damage;

        if (currentHealth <= 0.0f)
        {
            currentHealth = 0.0f;
            Die();
        }
    }
}