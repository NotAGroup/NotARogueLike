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
    private Rigidbody rigidBody;

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

    private bool initialize, initializedPhase = false;
    private Coroutine initializePhaseCoroutine;

    private int level = 0;

    private bool risen = false;
    private float riseDistance;
    private float riseDuration = 1.5f;
    private float riseTimer = 0.0f;

    private float roarDuration = 2.3f;

    private string selectedAttack;
    private float smallerDistance = 0.0f;

    private float jumpForwardForce = 12.0f;
    private float jumpUpForce = 5.0f;

    protected override void Start()
    {
        base.Start();

        if (hitZone != null)
        {
            hitZone.gameObject.SetActive(false);
        }

        currentPhase = Phase.None;

        fireBreath = GetComponentInChildren<FireBreath>();
        fireBreath.gameObject.SetActive(false);

        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        materials = meshRenderer.materials;
        rigidBody = GetComponent<Rigidbody>();

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
        if (selectedAttack == null)
        {
            yield break;
        }

        attacking = true;

        Debug.Log("Attack: " + selectedAttack);

        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;

        animator.SetTrigger(selectedAttack);

        if (selectedAttack == "breathFire")
        {
            yield return new WaitForSeconds(1.2f);
            fireBreath.gameObject.SetActive(true);

            yield return new WaitForSeconds(8.5f);
            fireBreath.gameObject.SetActive(false);
        }
        else
        {
            hitZone.SetDamage(stats.attackDamage);

            if (selectedAttack == "grab")
            {
                yield return new WaitForSeconds(1.4f);
                hitZone.gameObject.SetActive(true);

                yield return new WaitForSeconds(2.8f);
                hitZone.gameObject.SetActive(false);

                playerGotHit = false;
            }

            if (selectedAttack == "jumpAttack")
            {
                yield return new WaitForSeconds(0.15f);

                navMeshAgent.enabled = false;

                rigidBody.angularVelocity = Vector3.zero;
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

                Vector3 jumpForce = (transform.forward * jumpForwardForce) + (Vector3.up * jumpUpForce);
                rigidBody.AddForce(jumpForce, ForceMode.Impulse);

                yield return new WaitForSeconds(0.5f);
                hitZone.gameObject.SetActive(true);

                yield return new WaitUntil(IsGrounded);

                rigidBody.angularVelocity = Vector3.zero;
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.constraints = RigidbodyConstraints.FreezeAll;

                hitZone.gameObject.SetActive(false);
                
                playerGotHit = false;

                navMeshAgent.enabled = true;
            }

            if (selectedAttack == "punch")
            {
                yield return new WaitForSeconds(0.3f);
                hitZone.gameObject.SetActive(true);

                yield return new WaitForSeconds(1.8f);
                hitZone.gameObject.SetActive(false);

                playerGotHit = false;
            }

            if (selectedAttack == "swiping")
            {
                yield return new WaitForSeconds(0.06f);
                hitZone.gameObject.SetActive(true);

                yield return new WaitForSeconds(0.94f);
                hitZone.gameObject.SetActive(false);

                playerGotHit = false;
            }
        }

        attackCooldown = 1.0f / stats.attackRate;
        attacking = false;
    }

    protected override bool CanSeePlayer()
    {
        Vector3 playerPosition = playerTransform.position;
        bool inRoom = (playerPosition.x > bottomLeftAreaCorner.x && playerPosition.x < topRightAreaCorner.x &&
            playerPosition.z > bottomLeftAreaCorner.y && playerPosition.z < topRightAreaCorner.y);

        Vector3 direction = playerTransform.position - transform.position;
        bool inSight = (Vector3.Angle(transform.forward, direction.normalized) < viewAngle);

        return inRoom && inSight;
    }

    protected override void Combat()
    {
        if (!CanSeePlayer())
        {
            memoryTimer -= Time.deltaTime;

            if (memoryTimer <= 0f)
            {
                DestroyAllMinions();

                memoryTimer = 0.0f;
                aggressionModifier = 1.0f;
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

        if (!initializedPhase)
        {
            if (initialize)
            {
                return;
            }

            if (initializePhaseCoroutine != null)
            {
                StopCoroutine(initializePhaseCoroutine);
                initializedPhase = true;
            }
            else
            {
                initializePhaseCoroutine = StartCoroutine(InitializePhase());
            }
        }

        if (navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.updateRotation = false;
        }

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);

        Vector3 playerOppDir = (transform.position - playerTransform.position).normalized;
        float modifier = stats.minDistanceToPlayer * 0.8f;

        if (navMeshAgent.enabled)
        {
            navMeshAgent.SetDestination(playerTransform.position + playerOppDir * modifier);
        }

        if (attackCooldown == 0.0f && initializedPhase
            && distance <= stats.attackRange * aggressionModifier)
        {
            SelectAttack(distance);

            if (selectedAttack != null)
            {
                Attack();
            }
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

        if (navMeshAgent.enabled)
        {
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
        }

        direction = (aimPoints[0].transform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);
    }

    public override void TakeDamage(float damage, Vector3? direction = null)
    {
        if (risen && !initialize)
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

    private System.Collections.IEnumerator InitializePhase()
    {
        initialize = true;
        navMeshAgent.enabled = false;

        Debug.Log("Initialize phase: " + currentPhase);
        animator.SetTrigger("roar");

        yield return new WaitForSeconds(roarDuration);

        switch (currentPhase)
        {
            case Phase.One:
                aggressionModifier = 1.25f;
                //SpawnMeleeSkeletons(4);
                break;

            case Phase.Two:
                aggressionModifier = 1.5f;
                //SpawnRangedSkeletons(2);
                break;

            case Phase.Three:
                aggressionModifier = 1.75f;
                //SpawnMeleeSkeletons(4);
                //SpawnRangedSkeletons(2);
                break;
        }

        navMeshAgent.enabled = true;
        initialize = false;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.85f);
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

    private void SelectAttack(float distance)
    {
        float close = stats.minDistanceToPlayer * 1.5f;
        float medium = stats.attackRange * aggressionModifier * 0.5f;
        float far = stats.attackRange * aggressionModifier;

        selectedAttack = null;

        // Attacks: breathFire, grab, jumpAttack, punch, swiping

        if (distance <= close)
        {
            float attackChoice = Random.Range(0.0f, 1.0f);
            if(attackChoice < 0.33f)
            {
                selectedAttack = "grab";
            } else if(attackChoice < 0.67f)
            {
                selectedAttack = "punch";
            } else
            {
                selectedAttack = "swiping";
            }
        }

        if (distance <= medium)
        {
            if (currentPhase == Phase.Two || currentPhase == Phase.Three)
            {
                selectedAttack = "breathFire";
            }
        }

        if (distance <= far)
        {
            if (currentPhase == Phase.Two || currentPhase == Phase.Three)
            {
            }
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

            riseDistance = smallerDistance * 0.375f;

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
            initializedPhase = false;
            initializePhaseCoroutine = null;
        }
    }
}
