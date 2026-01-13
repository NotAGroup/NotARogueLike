using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(OpponentStats))]
public abstract class Opponent : MonoBehaviour
{
    // Components
    protected Animator animator;
    protected NavMeshAgent navMeshAgent;
    protected Transform playerTransform;
    protected Player player;
    protected OpponentStats stats;

    protected float currentHealth;

    protected bool attacking, hit, stunned, wandering = false;
    protected float attackCooldown, stunCooldown;
    protected float attackTimer, memoryTimer, wanderTimer;

    public Node spawnRoom;
    protected Vector3Int spawnRoomCenter;
    protected int nextNavPointID = 0;
    protected List<NavPoint> navPoints;

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
        wanderTimer = stats.wanderInterval;

        if (spawnRoom != null)
        {
            Vector2Int roomCenter = (spawnRoom.BottomLeftAreaCorner + spawnRoom.TopRightAreaCorner) / 2;
            spawnRoomCenter = new Vector3Int(roomCenter.x, 0, roomCenter.y);

            navPoints = spawnRoom.navPointList;
        }
    }

    protected virtual void Update()
    {
        if (currentHealth <= 0.0f || playerTransform == null)
        {
            return;
        }

        if (hit)
        {
            velocity = Vector3.zero;
            UpdateMovementAnimation();

            if (transform.rotation != rotation)
            {
                RotateTowards(direction);
                return;
            }

            if (!stunned && transform.rotation == rotation)
            {
                hit = false;
                stunned = true;
                stunCooldown = stats.stunDuration * 2.0f;
            }
        }

        if (stunned)
        {
            stunCooldown -= Time.deltaTime;

            if (stunCooldown <= 0.0f)
            {
                stunCooldown = 0.0f;
                stunned = false;

                navMeshAgent.isStopped = false;
            }

            return;
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

        if (CanSeePlayer() && !player.isDead)
        {
            memoryTimer = stats.memoryDuration;
        }


        if (memoryTimer > 0.0f)
        {
            memoryTimer -= Time.deltaTime;
            if (memoryTimer < 0.0f)
            {
                memoryTimer = 0.0f;
            }

            Combat();
        }
        else
        {
            Idle();
        }
    }

    protected abstract bool CanSeePlayer();

    protected abstract void Combat();

    protected virtual void Die()
    {
        navMeshAgent.isStopped = true;
        animator.SetBool("isDead", true);
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
            this.direction = direction.Value;
        }
        else
        {
            stunCooldown = stats.stunDuration;
            stunned = true;
        }

        currentHealth -= damage;

        if (currentHealth <= 0.0f)
        {
            currentHealth = 0.0f;
            Die();
        }

        hit = true;
    }
}