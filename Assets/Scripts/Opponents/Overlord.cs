using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Overlord : Opponent
{
    public enum Phase
    {
        None,
        One,
        Two,
        Three
    }

    private SkinnedMeshRenderer meshRenderer;
    private Material[] materials;

    private List<NavPoint> aimPoints;
    private int aimPointIndex = 0;

    public FireBreath fireBreath;
    public OpponentHitZone hitZone;

    public GameObject meleeSkeletonPrefab, rangedSkeletonPrefab;
    public Transform[] summonPoints;

    private Phase currentPhase;
    private Vector2Int bottomLeftAreaCorner, topRightAreaCorner;
    private Vector3 idlePosition;
    private bool initialized = false;

    private bool risen = false;
    private float riseDistance;
    private float riseDuration = 1.5f;
    private float riseTimer = 0.0f;

    private float jumpCooldown = 0.0f;
    private float jumpDistance = 10.0f;

    private float jumpTimer;

    protected override void Start()
    {
        base.Start();

        currentPhase = Phase.None;

        fireBreath = GetComponentInChildren<FireBreath>();
        fireBreath.gameObject.SetActive(false);

        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        materials = meshRenderer.materials;

        if (spawnRoom != null)
        {
            bottomLeftAreaCorner = spawnRoom.BottomLeftAreaCorner;
            topRightAreaCorner = spawnRoom.TopRightAreaCorner;

            Vector2Int roomCenter = (bottomLeftAreaCorner + topRightAreaCorner) / 2;
            spawnRoomCenter = new Vector3(roomCenter.x, 0.0f, roomCenter.y);

            aimPoints = spawnRoom.GetCorridorOpenings();
            navPoints = spawnRoom.GetCorners();
        }

        SetBlending(1.0f);
        TryGetIdlePosition(1.0f);
    }

    protected override System.Collections.IEnumerator AttackRoutine()
    {
        attacking = true;

        attacking = false;
        attackCoroutine = null;

        yield break;
    }

    protected override bool CanSeePlayer()
    {
        Vector3 playerPos = playerTransform.position;

        if (playerPos.x > bottomLeftAreaCorner.x && playerPos.x < topRightAreaCorner.x &&
            playerPos.z > bottomLeftAreaCorner.y && playerPos.z < topRightAreaCorner.y)
        {
            Vector3 direction = playerTransform.position - transform.position;
            return Vector3.Angle(transform.forward, direction.normalized) < viewAngle;
        }

        return false;
    }

    protected override void Combat()
    {
        if(!CanSeePlayer())
        {
            memoryTimer -= Time.deltaTime;

            if (memoryTimer <= 0f)
            {
                memoryTimer = 0.0f;
                currentPhase = Phase.None;
                state = OpponentState.Idle;
                return;
            }
        }
        else
        {
            memoryTimer = stats.memoryDuration;
        }

        UpdatePhase();

        if (!initialized)
        {
            InitializePhase();
            initialized = true;
        }

        navMeshAgent.SetDestination(playerTransform.position);

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (attackCooldown == 0f && distance <= stats.attackRange)
        {
            Attack();
        }
    }

    protected override void Idle()
    {
        if (CanSeePlayer() && !player.isDead)
        {
            float distance = Vector3.Distance(playerTransform.position, transform.position);

            if (distance <= riseDistance)
            {
                Rise();
            }

            if (risen)
            {
                memoryTimer = stats.memoryDuration;
                state = OpponentState.Combat;
            }

            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = true;

        Vector3 position = transform.position;
        position.y = 0.0f;

        // Move back to the center of the spawn room
        if (Vector3.Distance(position, idlePosition) > 1.0f)
        {
            navMeshAgent.SetDestination(idlePosition);
            return;
        }

        navMeshAgent.isStopped = true;

        direction = (aimPoints[0].transform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);
    }

    public override void TakeDamage(float damage, Vector3? direction = null)
    {
        if (risen)
        {
            // Ensure that overlord only takes damage after being resurrected
            base.TakeDamage(damage, direction);
        }
    }

    private void Rise()
    {
        if (!risen)
        {
            riseTimer += Time.deltaTime;

            float blend = riseTimer / riseDuration;
            SetBlending(Mathf.Lerp(1.0f, 0.0f, blend));

            if (blend >= 1.0f)
            {
                risen = true;
            }
        }
    }

    private void InitializePhase()
    {
        //animator.SetTrigger("Roar");

        switch (currentPhase)
        {
            case Phase.One:
                //SpawnMeleeSkeletons(5);
                break;

            case Phase.Two:
                //SpawnRangedSkeletons(4);
                break;

            case Phase.Three:
                //SpawnMeleeSkeletons(3);
                //SpawnRangedSkeletons(3);
                break;
        }
    }

    private void SetBlending(float value)
    {
        float blend = Mathf.Clamp01(value);

        foreach (Material material in materials)
        {
            material.SetFloat("_Blend", blend);
        }
    }

    private void TryGetIdlePosition(float maxDistance)
    {
        idlePosition = transform.position;

        if (spawnRoom != null)
        {
            float length = Mathf.Abs(bottomLeftAreaCorner.x - topRightAreaCorner.x);
            float width = Mathf.Abs(bottomLeftAreaCorner.y - topRightAreaCorner.y);

            float smallerDistance = Mathf.Min(width, length);
            float offset = smallerDistance * 0.25f;

            riseDistance =  smallerDistance * 0.375f;

            Vector3 direction = (aimPoints[0].transform.position - spawnRoomCenter).normalized;
            Vector3 position = spawnRoomCenter - direction * offset;
            
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                idlePosition = hit.position;
            }
        }
    }

    private void UpdatePhase()
    {
        float healthPercentage = currentHealth / stats.maxHealth;

        Phase nextPhase =
            healthPercentage > 0.66f ? Phase.One :
            healthPercentage > 0.33f ? Phase.Two : Phase.Three;

        if (nextPhase != currentPhase)
        {
            currentPhase = nextPhase;
            initialized = false;
        }
    }
}
