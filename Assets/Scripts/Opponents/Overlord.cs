
using UnityEngine;

public class Overlord : Opponent
{
    public enum Phase
    {
        One,
        Two,
        Three
    }

    private SkinnedMeshRenderer meshRenderer;
    private Material[] materials;

    public FireBreath fireBreath;
    public OpponentHitZone hitZone;

    public GameObject meleeSkeletonPrefab, rangedSkeletonPrefab;
    public Transform[] summonPoints;

    private Phase currentPhase;
    private bool initialized = false;

    private float jumpCooldown = 0.0f;
    private float jumpDistance = 10.0f;

    private float jumpTimer;

    protected override void Start()
    {
        base.Start();

        currentPhase = Phase.One;
        fireBreath = GetComponent<FireBreath>();
        
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        materials = meshRenderer.materials;

        if (spawnRoom != null)
        {
            Vector2Int roomCenter = (spawnRoom.BottomLeftAreaCorner + spawnRoom.TopRightAreaCorner) / 2;
            spawnRoomCenter = new Vector3(roomCenter.x, 0.0f, roomCenter.y);

            navPoints = spawnRoom.GetCorners();
        }

        foreach (Material material in materials)
        {
            material.SetFloat("_Blend", 1.0f);
        }
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
        return Vector3.Angle(transform.forward, direction.normalized) < viewAngle;
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
        throw new System.NotImplementedException();
    }

    private void InitializePhase()
    {
        animator.SetTrigger("Roar");

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
