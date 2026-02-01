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
    private Vector3 idlePosition;
    private bool initialized = false;

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
            Vector2Int roomCenter = (spawnRoom.BottomLeftAreaCorner + spawnRoom.TopRightAreaCorner) / 2;
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
        Vector3 direction = playerTransform.position - transform.position;
        //return Vector3.Angle(transform.forward, direction.normalized) < viewAngle;
        return false;
    }

    protected override void Combat()
    {
        UpdatePhase();

        if (!initialized)
        {
            InitializePhase();
            initialized = true;
        }

        navMeshAgent.SetDestination(playerTransform.position);

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (attackCooldown == 0f && distance <= stats.attackRange)
        {
            Attack();
        }
    }

    protected override void Idle()
    {
        if (CanSeePlayer() && !player.isDead)
        {
            memoryTimer = stats.memoryDuration;
            state = OpponentState.Combat;

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
            material.SetFloat("_Blend", 1.0f);
        }
    }

    private void TryGetIdlePosition(float maxDistance)
    {
        idlePosition = transform.position;

        if (spawnRoom != null)
        {
            Vector2Int bottomLeftAreaCorner = spawnRoom.BottomLeftAreaCorner;
            Vector2Int topRightAreaCorner = spawnRoom.TopRightAreaCorner;

            float length = bottomLeftAreaCorner.x - topRightAreaCorner.x;
            float width = bottomLeftAreaCorner.y - topRightAreaCorner.y;

            float offset = Mathf.Min(width, length) * 0.25f;
            Vector3 direction = (aimPoints[0].transform.position - spawnRoomCenter).normalized;

            Vector3 position = spawnRoomCenter + direction * offset;
            
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
