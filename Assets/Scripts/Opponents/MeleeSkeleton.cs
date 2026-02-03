using UnityEngine;
using UnityEngine.AI;

public class MeleeSkeleton : Opponent
{
    [Header("Combat")]
    public OpponentHitZone hitZone;

    protected override void Start()
    {
        base.Start();

        if (spawnRoom != null)
        {
            Vector2Int roomCenter = (spawnRoom.BottomLeftAreaCorner + spawnRoom.TopRightAreaCorner) / 2;
            spawnRoomCenter = new Vector3(roomCenter.x, 0.0f, roomCenter.y);

            navPoints = spawnRoom.GetCorners();
        }
    }

    protected override void Combat()
    {
        if (!CanSeePlayer() && navMeshAgent.remainingDistance <= 1f)
        {
            memoryTimer -= Time.deltaTime;

            if (memoryTimer <= 0f)
            {
                memoryTimer = 0.0f;
                state = OpponentState.Idle;
                return;
            }
        }
        else
        {
            memoryTimer = stats.memoryDuration;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = false;

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);

        Vector3 playerOppDir = (transform.position - playerTransform.position).normalized;
        float modifier = currentHealth < stats.maxHealth * 0.5f ? 7.5f : 2.5f;

        navMeshAgent.SetDestination(playerTransform.position + playerOppDir * modifier);

        if (attackCooldown == 0.0f && distance <= stats.attackRange * aggressionModifier)
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

        Wander();
        RegenerateHealth();
    }

    protected override System.Collections.IEnumerator AttackRoutine()
    {
        attacking = true;

        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;

        animator.SetFloat("AttackSpeed", stats.attackRate);
        animator.SetTrigger("swing");
        hitZone.SetDamage(stats.attackDamage);

        yield return new WaitForSeconds(stats.swingDuration / stats.attackRate);
        hitZone.gameObject.SetActive(true);

        yield return new WaitForSeconds(stats.strikeDuration / stats.attackRate);
        hitZone.gameObject.SetActive(false);

        navMeshAgent.isStopped = false;
        playerGotHit = false;

        if (CanSeePlayer() && !player.isDead)
        {
            memoryTimer = stats.memoryDuration;
            state = OpponentState.Combat;
        }
        else
        {
            state = OpponentState.Idle;
        }

        attacking = false;
        attackCooldown = 1.0f / stats.attackRate;
        attackCoroutine = null;
    }

    void Wander()
    {
        if (navMeshAgent.remainingDistance < 1)
        {
            wanderTimer -= Time.deltaTime;

            // Look towards the center of the spawn room
            direction = spawnRoomCenter - transform.position;
            direction.y = 0.0f;

            rotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rotation,
                Time.deltaTime * stats.rotationSpeed
            );

            if (wanderTimer <= 0.0f)
            {
                wandering = false;
            }
        }

        if (!wandering && navPoints != null)
        {
            Vector3 targetPos = navPoints[nextNavPointID].transform.position;

            NavMeshHit navHit;
            NavMesh.SamplePosition(targetPos, out navHit, stats.wanderRadius, NavMesh.AllAreas);

            navMeshAgent.SetDestination(navHit.position);

            wanderTimer = Random.Range(stats.wanderInterval * 0.5f, stats.wanderInterval * 2.0f);
            wandering = true;

            nextNavPointID = (nextNavPointID + 1) % navPoints.Count;
        }
    }

    protected override bool CanSeePlayer()
    {
        if (playerTransform == null)
        {
            return false;
        }

        Vector3 direction = playerTransform.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stats.detectionRange)
        {
            return false;
        }

        if (distance < stats.alertRange && !player.isSneaking())
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, direction.normalized);

        if (angle > viewAngle)
        {
            return false;
        }

        if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, stats.detectionRange))
        {
            return hit.transform.CompareTag("Player");
        }

        return false;
    }
}
