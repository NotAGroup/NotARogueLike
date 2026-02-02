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

    private Transform area;
    private OpponentDefinitions opponentDefinitions;

    private List<NavPoint> aimPoints;
    private readonly HashSet<GameObject> minions = new();

    public FireBreath fireBreath;
    public OpponentHitZone hitZone;

    public GameObject meleeSkeletonPrefab, rangedSkeletonPrefab;
    public Transform[] summonPoints;

    private Phase currentPhase;
    private Vector2Int bottomLeftAreaCorner, topRightAreaCorner;
    private Vector3 idlePosition;
    private bool initialized = false;

    private int level = 0;

    private bool risen = false;
    private float riseDistance;
    private float riseDuration = 1.5f;
    private float riseTimer = 0.0f;

    private float smallerDistance = 0.0f;

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

        opponentDefinitions = GameObject.Find("Definitions").GetComponent<OpponentDefinitions>();

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
                DestroyAllMinions();
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
    private void DestroyAllMinions()
    {
        foreach (GameObject minion in minions)
        {
            DestroyImmediate(minion);
        }
    }

    private void InitializePhase()
    {
        //animator.SetTrigger("Roar");

        switch (currentPhase)
        {
            case Phase.One:
                SpawnMeleeSkeletons(4);
                break;

            case Phase.Two:
                SpawnRangedSkeletons(4);
                break;

            case Phase.Three:
                SpawnMeleeSkeletons(4);
                SpawnRangedSkeletons(2);
                break;
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

    private void SetBlending(float value)
    {
        float blend = Mathf.Clamp01(value);

        foreach (Material material in materials)
        {
            material.SetFloat("_Blend", blend);
        }
    }

    public void SetArea(Transform area)
    {
        this.area = area;
    }

    public void SetLevel(int level)
    {
        this.level = level;
    }

    private void SpawnMeleeSkeletons(int value)
    {
        OpponentClassDefinition opponentClass = opponentDefinitions["Melee Skeleton"];
        int index = 0;

        for (int i = 0; i < value; i++)
        {
            Vector3 position = navPoints[index].transform.position;
            index = (index + 1) % navPoints.Count;

            GameObject instance = Instantiate(meleeSkeletonPrefab, position, Quaternion.identity, area);
            instance.name = meleeSkeletonPrefab.name;

            if (instance.TryGetComponent<MeleeSkeleton>(out MeleeSkeleton skeleton))
            {
                skeleton.SetNavPointID(index);
            }

            if (instance.TryGetComponent<Opponent>(out Opponent opponent))
            {
                opponent.spawnRoom = spawnRoom;
            }

            if (instance.TryGetComponent<OpponentStats>(out OpponentStats stats))
            {
                stats.ComputeFrom(opponentClass, level);
            }

            minions.Add(instance);
        }
    }

    private void SpawnRangedSkeletons(int value)
    {
        OpponentClassDefinition opponentClass = opponentDefinitions["Ranged Skeleton"];
        int index = 0;

        for (int i = 0; i < value; i++)
        {
            Vector3 direction;

            switch (index)
            {
                case 0:
                    direction = Vector3.left;
                    break;
                case 1:
                    direction = Vector3.right;
                    break;
                case 2:
                    direction = Vector3.back;
                    break;
                case 3:
                    direction = Vector3.forward;
                    break;
                default:
                    direction = Vector3.back;
                    break;
            }

            Vector3 position = spawnRoomCenter + direction * (smallerDistance * 0.5f - 2.5f);
            position.y = 1.0f;
            index = (index + 1) % 4;

            GameObject instance = Instantiate(rangedSkeletonPrefab, position, Quaternion.identity, area);
            instance.name = rangedSkeletonPrefab.name;

            if (instance.TryGetComponent<RangedSkeleton>(out RangedSkeleton skeleton))
            {
                skeleton.SetIdlePosition(position);
            }

            if (instance.TryGetComponent<Opponent>(out Opponent opponent))
            {
                opponent.spawnRoom = spawnRoom;
            }

            if (instance.TryGetComponent<OpponentStats>(out OpponentStats stats))
            {
                stats.ComputeFrom(opponentClass, level);
            }

            minions.Add(instance);
        }
    }

    private void TryGetIdlePosition(float maxDistance)
    {
        idlePosition = transform.position;

        if (spawnRoom != null)
        {
            float height = topRightAreaCorner.y - bottomLeftAreaCorner.y;
            float width = topRightAreaCorner.x - bottomLeftAreaCorner.x;

            smallerDistance = Mathf.Min(height, width);
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
