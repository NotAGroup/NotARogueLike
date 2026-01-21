using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RangedSkeleton : Opponent
{
    [Header("Combat")]
    public GameObject ammunition;

    private List<NavPoint> aimPoints;
    private int aimPointIndex = 0;
    private float aimDuration = 4.0f;
    private float aimTimer;

    private float avoidRadius = 1.2f;
    private float minDistance = 3.0f;

    private float shootDuration = 0.3f;
    private float shootRate = 5.0f;

    private Vector3 idlePosition;

    protected override void Start()
    {
        base.Start();

        navMeshAgent.SetAreaCost(
            NavMesh.GetAreaFromName("Avoid Player"),
            25.0f);

        navMeshAgent.avoidancePriority = 30;
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        if (spawnRoom != null)
        {
            Vector2Int roomCenter = (spawnRoom.BottomLeftAreaCorner + spawnRoom.TopRightAreaCorner) / 2;
            spawnRoomCenter = new Vector3(roomCenter.x, 0.0f, roomCenter.y);

            aimPoints = spawnRoom.GetCorridorOpenings();
            navPoints = spawnRoom.GetCorners();
        }

        TryGetIdlePosition(2.0f);
    }

    protected override void Update()
    {
        base.Update();

        if (attacking)
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= shootDuration)
            {
                attacking = false;
                attackCooldown = 1.0f / shootRate;
            }

            return;
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

    protected override void Combat()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = false;

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);

        if (distance < minDistance && !wandering)
        {
            NavPoint escapePoint = GetNextNavPoint();

            if (escapePoint != null)
            {
                navMeshAgent.SetDestination(escapePoint.transform.position);
                wandering = true;
            }
            return;
        }

        if (wandering && navMeshAgent.remainingDistance < 1.0f)
        {
            wandering = false;
            return;
        }

        if (!attacking)
        {
            if (attackCooldown == 0.0f && distance <= stats.attackRange)
            {
                Attack();
            }
        }

    }

    protected override void Idle()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = true;

        RegenerateHealth();

        Vector3 position = transform.position;
        position.y = 0.0f;

        // Move back to the center of the spawn room
        if (Vector3.Distance(position, idlePosition) > 1.0f)
        {
            navMeshAgent.SetDestination(idlePosition);
            return;
        }

        navMeshAgent.isStopped = true;

        // Aim towards the various openings in the room
        if (aimPoints == null)
        {
            return;
        }

        aimTimer -= Time.deltaTime;

        if (aimTimer <= 0.0f)
        {
            aimPointIndex = (aimPointIndex + 1) % aimPoints.Count;
            aimTimer = aimDuration;
        }

        direction = (aimPoints[aimPointIndex].transform.position - transform.position).normalized;
        direction.y = 0.0f;

        RotateTowards(direction);
    }

    void Attack()
    {
        attacking = true;
        attackTimer = 0.0f;

        navMeshAgent.isStopped = true;
        animator.SetTrigger("shoot");
    }

    private NavPoint GetNextNavPoint()
    {
        NavPoint nextNavPoint = null;
        float maxScore = float.NegativeInfinity;

        Vector3 playerPosition = playerTransform.position;

        foreach (NavPoint navPoint in navPoints)
        {
            Vector3 navPointPosition = navPoint.transform.position;

            if (Vector3.Distance(navPointPosition, playerPosition) < minDistance || PathCrossesPlayer(navPointPosition))
            {
                continue;
            }

            Vector3 toPlayer = (playerPosition - transform.position).normalized;
            Vector3 toPoint = (navPointPosition - transform.position).normalized;

            float score = Vector3.Dot(Vector3.Cross(toPlayer, Vector3.up), toPoint);

            if (maxScore < score)
            {
                maxScore = score;
                nextNavPoint = navPoint;
            }
        }

        return nextNavPoint;
    }

    private bool PathCrossesPlayer(Vector3 target)
    {
        NavMeshPath path = new NavMeshPath();

        if (!navMeshAgent.CalculatePath(target, path))
        {
            return true;
        }

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 corner = path.corners[i];
            Vector3 nextCorner = path.corners[i + 1];

            Vector3 direction = nextCorner - corner;
            float factor = Mathf.Clamp01(Vector3.Dot(playerTransform.position - corner, direction) / direction.sqrMagnitude);

            Vector3 closest = corner + direction * factor;

            if (Vector3.Distance(playerTransform.position, closest) < avoidRadius)
            {
                return true;
            }
        }

        return false;
    }

    private void TryGetIdlePosition(float maxRadius)
    {
        idlePosition = transform.position;

        if (spawnRoom != null)
        {
            if (NavMesh.SamplePosition(spawnRoomCenter, out NavMeshHit hit, 0.25f, NavMesh.AllAreas))
            {
                idlePosition = hit.position;
            }

            const int radialSteps = 3;
            const int angleSteps = 12;

            for (int r = 1; r <= radialSteps; r++)
            {
                float radius = (maxRadius / radialSteps) * r;

                for (int a = 0; a < angleSteps; a++)
                {
                    float angle = (360.0f / angleSteps) * a * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle),
                        0.0f,
                        Mathf.Sin(angle)
                        ) * radius;

                    if (NavMesh.SamplePosition(spawnRoomCenter + offset, out hit, 0.25f, NavMesh.AllAreas))
                    {
                        idlePosition = hit.position;
                    }
                }
            }
        }
    }
}
