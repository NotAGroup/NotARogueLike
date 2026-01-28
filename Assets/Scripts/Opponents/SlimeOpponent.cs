using UnityEngine;
using UnityEngine.AI;

public class SlimeOpponent : Opponent
{
    [Header("Combat")]
    public OpponentHitZone hitZone;

    [SerializeField] private float leapDuration = 0.55f;
    [SerializeField] private float leapHeight = 0.9f;
    [SerializeField] private float damageWindowStart = 0.10f;
    [SerializeField] private float damageWindowEnd = 0.38f;

    [Header("Visual")]
    [SerializeField] private Renderer slimeRenderer;
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.2f, 1f);

    private MaterialPropertyBlock mpb;
    private Color normalBaseColor;
    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");

    private bool aggroed;

    private Vector3 leapStart;
    private Vector3 leapTarget;

    protected override void Start()
    {
        base.Start();

        aggroed = false;

        if (hitZone != null)
        {
            hitZone.gameObject.SetActive(false);
            hitZone.SetDamage(stats.attackDamage);
        }

        if (slimeRenderer == null)
            slimeRenderer = GetComponentInChildren<Renderer>();

        if (slimeRenderer != null)
        {
            mpb = new MaterialPropertyBlock();

            if (slimeRenderer.sharedMaterial != null && slimeRenderer.sharedMaterial.HasProperty(ID_BaseColor))
                normalBaseColor = slimeRenderer.sharedMaterial.GetColor(ID_BaseColor);
            else
                normalBaseColor = Color.white;
        }

        navMeshAgent.updateRotation = false;
    }

    protected override void Update()
    {
        if (currentHealth <= 0.0f || playerTransform == null) return;

        if (hit)
        {
            velocity = Vector3.zero;
            UpdateMovementAnimation();

            if (Quaternion.Angle(transform.rotation, rotation) > 1.0f)
            {
                RotateTowards(direction);
                return;
            }

            if (!stunned)
            {
                hit = false;
                stunned = true;
                stunCooldown = stats.stunDuration * 2.0f;
                navMeshAgent.isStopped = false;
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

        if (attackCooldown > 0.0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0.0f) attackCooldown = 0.0f;
        }

        if (!aggroed && CanSeePlayer() && !player.isDead)
            aggroed = true;

        if (attacking)
        {
            AttackUpdate();
            return;
        }

        if (aggroed && !player.isDead)
            Combat();
        else
            Idle();

        velocity = transform.InverseTransformDirection(navMeshAgent.velocity);
        UpdateMovementAnimation();
    }

    protected override bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > stats.detectionRange) return false;

        if (distance < 5.0f && !player.isSneaking())
            return true;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle) return false;

        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hitInfo, stats.detectionRange))
            return hitInfo.transform.CompareTag("Player");

        return false;
    }

    protected override void Combat()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = false;

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0.0f;

        if (direction.sqrMagnitude > 1e-6f)
            RotateTowards(direction);

        if (!attacking)
        {
            navMeshAgent.SetDestination(playerTransform.position);

            if (attackCooldown == 0.0f && distance <= stats.attackRange)
                StartAttack();
        }
    }

    protected override void Idle()
    {
        navMeshAgent.isStopped = true;
        RegenerateHealth();
    }

    private void StartAttack()
    {
        attacking = true;
        attackTimer = 0.0f;
        playerGotHit = false;

        navMeshAgent.isStopped = true;

        leapStart = transform.position;
        leapTarget = playerTransform.position;
        leapTarget.y = leapStart.y;

        if (hitZone != null)
        {
            hitZone.SetDamage(stats.attackDamage);
            hitZone.gameObject.SetActive(false);
        }

        SetAttackTint(true);
    }

    private void AttackUpdate()
    {
        attackTimer += Time.deltaTime;

        float t = Mathf.Clamp01(attackTimer / Mathf.Max(0.0001f, leapDuration));

        Vector3 pos = Vector3.Lerp(leapStart, leapTarget, t);
        float arc = 4f * t * (1f - t);
        pos.y = leapStart.y + arc * leapHeight;
        transform.position = pos;

        Vector3 flat = (leapTarget - leapStart);
        flat.y = 0f;
        if (flat.sqrMagnitude > 1e-6f)
            RotateTowards(flat.normalized);

        bool inDamage = attackTimer >= damageWindowStart && attackTimer <= damageWindowEnd;
        if (hitZone != null)
            hitZone.gameObject.SetActive(inDamage);

        if (attackTimer >= leapDuration)
        {
            if (hitZone != null)
                hitZone.gameObject.SetActive(false);

            attacking = false;
            attackCooldown = 1.0f / Mathf.Max(0.0001f, stats.hitRate);

            navMeshAgent.Warp(transform.position);
            navMeshAgent.isStopped = false;

            SetAttackTint(false);
        }
    }

    private void SetAttackTint(bool on)
    {
        if (slimeRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();

        slimeRenderer.GetPropertyBlock(mpb);

        Color baseCol = normalBaseColor;
        float alpha = baseCol.a;

        Color c = on ? attackColor : baseCol;
        c.a = alpha;

        mpb.SetColor(ID_BaseColor, c);
        slimeRenderer.SetPropertyBlock(mpb);
    }
}
